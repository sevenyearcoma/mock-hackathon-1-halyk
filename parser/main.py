"""
Banking Compliance Document → JSON Pipeline
Layers 1–3: Intake → Extraction → LLM Parsing
LLM backend: GitHub Models (OpenAI-compatible API)
"""

import hashlib
import json
import os
import tempfile
from datetime import datetime
from pathlib import Path
from typing import Optional

import pdfplumber
from dotenv import load_dotenv
from fastapi import FastAPI, File, Form, HTTPException, UploadFile
from fastapi.responses import JSONResponse
from openai import OpenAI

load_dotenv()

# ---------------------------------------------------------------------------
# App setup
# ---------------------------------------------------------------------------

app = FastAPI(
    title="Compliance Document Parser",
    description="Layers 1–3: Intake → Text Extraction → LLM Structured Parsing",
    version="1.0.0",
)

# ---------------------------------------------------------------------------
# GitHub Models client (OpenAI-compatible)
# ---------------------------------------------------------------------------

GITHUB_TOKEN = os.get("GITHUB_TOKEN")
GITHUB_MODEL = os.get("GITHUB_MODEL", "openai/gpt-4o-mini")

github_client = OpenAI(
    base_url="https://models.github.ai/inference",
    api_key=GITHUB_TOKEN,
)

# ---------------------------------------------------------------------------
# Compliance JSON schema (injected into the LLM prompt)
# ---------------------------------------------------------------------------

INVOICE_SCHEME = """\
{
    "inv_number": "INV-2026-0004",
    "inv_date": "28 Jun 2026",
    "inv_due_date": "12 Jul 2026",
    "inv_currency": "KZT",
    "inv_subtotal": 2500000,
    "inv_vat": 0,
    "inv_total": 2500000,
    "inv_seller_name": "AlmaTech Solutions LLP",
    "inv_seller_bin": "240540012345",
    "inv_seller_bank": "Halyk Bank Kazakhstan JSC",
    "inv_seller_iban": "KZ859650000012345678",
    "inv_buyer_name": "Nomad Digital LLP",
    "inv_buyer_bin": "241080054321",
    "inv_contract_no": "ND-AS-07/2026",
    "inv_contract_date": "2025-12-19"
}"""

PAYMENT_ORDER_SCHEME = """
{
    "pay_date": "02 Jul 2026",
    "pay_payer_name": "Nomad Digital LLP",
    "pay_payer_bin": "241080054321",
    "pay_payer_bank": "ForteBank JSC",
    "pay_payer_iban": "KZ246010000054321098",
    "pay_receiver_name": "AlmaTech Solutions LLP",
    "pay_receiver_bin": "240540012345",
    "pay_receiver_bank": "Halyk Bank Kazakhstan JSC",
    "pay_receiver_iban": "KZ859650000012345678",
    "pay_amount": 2500000,
    "pay_currency": "KZT",
    "pay_purpose": "Payment for marketing campaign development under Contract No. ND-AS-07/2026, Invoice No. INV-2026-0004"
}
"""

ORDER_SYSTEM_PROMPT = f"""\
You are a compliance invoice document parser for a bank.
Extract data strictly into this JSON schema:

{PAYMENT_ORDER_SCHEME}

Rules:
- Return ONLY valid JSON — no markdown fences, no explanation, no preamble.
- Use null for missing fields; never omit keys that appear in the schema.
- Normalize all dates to YYYY-MM-DD format.
- In risk_indicators, list any suspicious or notable observations as short strings.
- In fields, capture any key-value pairs present in the document that don't fit the schema above.
"""

INVOICE_SYSTEM_PROMPT = f"""\
You are a compliance invoice document parser for a bank.
Extract data strictly into this JSON schema:

{INVOICE_SCHEME}

Rules:
- Return ONLY valid JSON — no markdown fences, no explanation, no preamble.
- Use null for missing fields; never omit keys that appear in the schema.
- Normalize all dates to YYYY-MM-DD format.
- In risk_indicators, list any suspicious or notable observations as short strings.
- In fields, capture any key-value pairs present in the document that don't fit the schema above.
"""

DOCUMENT_PROMPTS = {
    "invoice": INVOICE_SYSTEM_PROMPT,
    "payment_order": ORDER_SYSTEM_PROMPT,
}

# ---------------------------------------------------------------------------
# Layer 1 — Intake & Preprocessing
# ---------------------------------------------------------------------------


def ingest_document(file_bytes: bytes, filename: str) -> dict:
    """Hash the file and detect whether it has a text layer."""
    file_hash = hashlib.sha256(file_bytes).hexdigest()

    # Write to a temp file so pdfplumber can open it
    with tempfile.NamedTemporaryFile(suffix=".pdf", delete=False) as tmp:
        tmp.write(file_bytes)
        tmp_path = tmp.name

    try:
        with pdfplumber.open(tmp_path) as pdf:
            page_count = len(pdf.pages)
            # Check first page for a text layer
            first_page_text = pdf.pages[0].extract_text() if page_count > 0 else ""
            has_text_layer = bool(first_page_text and first_page_text.strip())
    finally:
        os.unlink(tmp_path)

    return {
        "file_hash": file_hash,
        "filename": filename,
        "is_scanned": not has_text_layer,
        "page_count": page_count,
        "tmp_written": False,  # handled above
        "tmp_path": None,
    }


# ---------------------------------------------------------------------------
# Layer 2 — Text Extraction (dual path)
# ---------------------------------------------------------------------------


def extract_text_digital(file_bytes: bytes) -> list[dict]:
    """Extract text from a digital (non-scanned) PDF via pdfplumber."""
    pages = []
    with tempfile.NamedTemporaryFile(suffix=".pdf", delete=False) as tmp:
        tmp.write(file_bytes)
        tmp_path = tmp.name

    try:
        with pdfplumber.open(tmp_path) as pdf:
            for i, page in enumerate(pdf.pages):
                text = page.extract_text() or ""
                tables = page.extract_tables() or []
                # Flatten tables into readable text blocks
                table_text = ""
                for table in tables:
                    for row in table:
                        row_clean = [cell or "" for cell in row]
                        table_text += " | ".join(row_clean) + "\n"

                pages.append(
                    {
                        "page": i + 1,
                        "text": text,
                        "tables": tables,
                        "table_text": table_text.strip(),
                        "method": "pdfplumber",
                        "confidence": 1.0,  # native text layer — always perfect
                    }
                )
    finally:
        os.unlink(tmp_path)

    return pages


def extract_text_ocr(file_bytes: bytes) -> list[dict]:
    """
    OCR path for scanned PDFs.
    Uses pdf2image + pytesseract (CPU). Swap for PaddleOCR on GPU in prod.
    """
    try:
        import pytesseract
        from pdf2image import convert_from_bytes
    except ImportError:
        raise HTTPException(
            status_code=500,
            detail="OCR dependencies not installed. Run: pip install pdf2image pytesseract",
        )

    pages = []
    images = convert_from_bytes(file_bytes, dpi=200)

    for i, img in enumerate(images):
        ocr_data = pytesseract.image_to_data(img, output_type=pytesseract.Output.DICT)
        words = [w for w in ocr_data["text"] if w.strip()]
        confs = [
            c
            for c, w in zip(ocr_data["conf"], ocr_data["text"])
            if w.strip() and c != -1
        ]
        avg_conf = round(sum(confs) / len(confs) / 100, 3) if confs else 0.0
        full_text = pytesseract.image_to_string(img)

        pages.append(
            {
                "page": i + 1,
                "text": full_text,
                "tables": [],
                "table_text": "",
                "method": "pytesseract_ocr",
                "confidence": avg_conf,
            }
        )

    return pages


def build_combined_text(pages: list[dict]) -> str:
    """Merge all page texts (plus any table text) into one string for the LLM."""
    parts = []
    for p in pages:
        parts.append(f"--- Page {p['page']} ---")
        if p["text"]:
            parts.append(p["text"])
        if p.get("table_text"):
            parts.append("[TABLE]\n" + p["table_text"])
    return "\n".join(parts)


# ---------------------------------------------------------------------------
# Layer 3 — LLM Structured Parsing via GitHub Models
# ---------------------------------------------------------------------------


def parse_with_llm(
    text: str,
    document_type: str,
    model: str = GITHUB_MODEL,
) -> dict:
    """Send extracted text to GitHub Models and get back structured JSON."""

    if not GITHUB_TOKEN:
        raise HTTPException(
            status_code=500,
            detail="GITHUB_TOKEN environment variable is not set.",
        )

    system_prompt = DOCUMENT_PROMPTS.get(document_type)

    if not system_prompt:
        raise HTTPException(
            status_code=400,
            detail=f"Unsupported document type: {document_type}",
        )

    response = github_client.chat.completions.create(
        model=model,
        temperature=0.0,
        max_tokens=2000,
        messages=[
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": text},
        ],
    )

    raw = response.choices[0].message.content or ""

    # Strip markdown fences
    raw = raw.strip()

    if raw.startswith("```"):
        raw = raw.split("```")[1]

        if raw.startswith("json"):
            raw = raw[4:]

    raw = raw.strip().rstrip("`").strip()

    try:
        parsed = json.loads(raw)

    except json.JSONDecodeError as exc:
        raise HTTPException(
            status_code=502,
            detail=f"LLM returned non-JSON output: {exc}. Raw: {raw[:300]}",
        )

    return parsed


# ---------------------------------------------------------------------------
# API Endpoints
# ---------------------------------------------------------------------------


@app.get("/health")
def health():
    return {"status": "ok", "model": GITHUB_MODEL}


@app.post("/parse", summary="Parse a compliance PDF and return structured JSON")
async def parse_compliance_document(
    file: UploadFile = File(..., description="PDF compliance document"),
    document_type: str = Form(...),
    model: Optional[str] = Form(
        default=None,
        description=f"GitHub Models model name (default: {GITHUB_MODEL})",
    ),
):
    """
    Upload a compliance document (PDF).

    Returns structured JSON extracted through:
    - **Layer 1** — Intake: file hash + scanned/digital detection
    - **Layer 2** — Extraction: pdfplumber (digital) or pytesseract OCR (scanned)
    - **Layer 3** — Parsing: GitHub Models LLM → strict compliance JSON schema
    """
    if not file.filename or not file.filename.lower().endswith(".pdf"):
        raise HTTPException(status_code=400, detail="Only PDF files are supported.")

    file_bytes = await file.read()
    if not file_bytes:
        raise HTTPException(status_code=400, detail="Uploaded file is empty.")

    started_at = datetime.utcnow()

    # --- Layer 1: Intake ---
    intake = ingest_document(file_bytes, file.filename)

    # --- Layer 2: Extraction ---
    if intake["is_scanned"]:
        pages = extract_text_ocr(file_bytes)
    else:
        pages = extract_text_digital(file_bytes)

    combined_text = build_combined_text(pages)

    if not combined_text.strip():
        raise HTTPException(
            status_code=422,
            detail="No text could be extracted from the document.",
        )

    # --- Layer 3: LLM Parsing ---
    llm_model = model or GITHUB_MODEL
    parsed_json = parse_with_llm(
        combined_text, document_type=document_type, model=llm_model
    )

    # --- Response envelope ---
    avg_confidence = (
        round(sum(p["confidence"] for p in pages) / len(pages), 3) if pages else 0.0
    )

    low_confidence = avg_confidence < 0.80 and intake["is_scanned"]

    return JSONResponse(
        content={
            "status": "ok",
            "requires_human_review": low_confidence,
            "pipeline": {
                "layer1_intake": {
                    "file_hash": intake["file_hash"],
                    "filename": intake["filename"],
                    "page_count": intake["page_count"],
                    "is_scanned": intake["is_scanned"],
                },
                "layer2_extraction": {
                    "method": pages[0]["method"] if pages else "none",
                    "pages_extracted": len(pages),
                    "avg_confidence": avg_confidence,
                    "low_confidence_flag": low_confidence,
                },
                "layer3_parsing": {
                    "llm_model": llm_model,
                    "provider": "github_models",
                },
            },
            "parsed_document": parsed_json,
            "meta": {
                "processed_at": started_at.isoformat() + "Z",
                "processing_seconds": round(
                    (datetime.utcnow() - started_at).total_seconds(), 2
                ),
            },
        }
    )


@app.post(
    "/parse/text",
    summary="Parse raw text (no PDF needed) — useful for testing",
)
async def parse_raw_text(
    text: str = Form(..., description="Raw compliance document text"),
    document_type: str = Form(...),
    model: Optional[str] = Form(default=None),
):
    """
    Send raw extracted text directly to the LLM parsing layer (Layer 3 only).
    Skips Layers 1 & 2 — handy for testing the LLM output.
    """
    llm_model = model or GITHUB_MODEL
    parsed_json = parse_with_llm(
        text,
        document_type=document_type,
        model=llm_model,
    )
    return JSONResponse(content={"status": "ok", "parsed_document": parsed_json})
