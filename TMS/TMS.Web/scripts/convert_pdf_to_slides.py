import fitz  # PyMuPDF
import sys
import os

def convert_pdf_to_slides(pdf_path, out_dir):
    """
    Splits a PDF file into sequential PNG images using PyMuPDF.
    Names them slide_1.png, slide_2.png, etc., to match TMS slide conventions.
    """
    try:
        if not os.path.exists(out_dir):
            os.makedirs(out_dir, exist_ok=True)

        # Open the source PDF file
        doc = fitz.open(pdf_path)
        
        # Increase resolution scale for crisp font/shape rendering (2x DPI)
        zoom = 2.0
        mat = fitz.Matrix(zoom, zoom)

        count = 0
        for i, page in enumerate(doc):
            # Render page image
            pix = page.get_pixmap(matrix=mat, alpha=False)
            
            # Write destination file matching 'slide_{n}.png'
            dest_path = os.path.join(out_dir, f"slide_{i + 1}.png")
            pix.save(dest_path)
            count += 1

        doc.close()
        print(f"CONVERSION_COMPLETE: {count} slides written.")
        return count
    except Exception as e:
        print(f"CONVERSION_ERROR: {str(e)}", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("ERROR: Insufficient arguments. Usage: python convert_pdf_to_slides.py <pdf_path> <output_dir>", file=sys.stderr)
        sys.exit(1)
        
    source_pdf = sys.argv[1]
    target_dir = sys.argv[2]
    
    if not os.path.isfile(source_pdf):
        print(f"ERROR: PDF source file not found at {source_pdf}", file=sys.stderr)
        sys.exit(1)
        
    convert_pdf_to_slides(source_pdf, target_dir)
