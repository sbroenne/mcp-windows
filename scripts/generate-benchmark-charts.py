"""Generate GitHub-readable SVG images from the website's shared chart markup."""

from html import escape
from pathlib import Path
from textwrap import wrap
import xml.etree.ElementTree as ET


CHART_DIRECTORY = Path(__file__).resolve().parents[1] / "gh-pages" / "docs" / "assets" / "charts"


def render_svg(source: Path) -> str:
    chart = ET.parse(source).getroot()
    maximum = float(chart.attrib["style"].split(":")[1])
    title = chart.findtext("figcaption")
    note = chart.findtext("p")
    title_lines = wrap(title, width=38)
    note_lines = wrap(note, width=52)
    rows = chart.findall("ul/li")
    description = note + " " + "; ".join(
        row.findtext("span[@class='benchmark-chart__label']") + ": "
        + row.findtext("span[@class='benchmark-chart__value']")
        for row in rows
    )
    top = 34 + len(title_lines) * 24
    axis_y = top + len(rows) * 62
    height = axis_y + 36 + len(note_lines) * 20
    elements = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="480" height="{height}" '
        f'viewBox="0 0 480 {height}" role="img" aria-labelledby="title description">',
        f"<title id=\"title\">{escape(title)}</title>",
        f"<desc id=\"description\">{escape(description)}</desc>",
        f'<rect width="480" height="{height}" rx="8" fill="#ffffff"/>',
        '<g font-family="Arial, sans-serif" fill="#24292f">',
    ]

    def text(x, y, value, size=18, anchor="start", weight="normal"):
        elements.append(
            f'<text x="{x}" y="{y}" font-size="{size}" text-anchor="{anchor}" '
            f'font-weight="{weight}">{escape(value)}</text>'
        )

    for index, line in enumerate(title_lines):
        text(20, 30 + index * 24, line, weight="bold")
    for index, row in enumerate(rows):
        value = float(row.attrib["style"].split(":")[1])
        if not 0 <= value <= maximum:
            raise ValueError(f"Chart value {value} is outside the scale in {source}")
        label = row.findtext("span[@class='benchmark-chart__label']")
        display_value = row.findtext("span[@class='benchmark-chart__value']")
        y = top + index * 62
        text(20, y, label)
        text(460, y, display_value, anchor="end", weight="bold")
        elements.append(f'<rect x="20" y="{y + 12}" width="440" height="12" rx="3" fill="#eaeef2"/>')
        elements.append(
            f'<rect x="20" y="{y + 12}" width="{440 * value / maximum:.2f}" '
            'height="12" rx="3" fill="#0078d4"/>'
        )
    axis = chart.findall("div/span")
    text(20, axis_y, axis[0].text, size=14)
    text(460, axis_y, axis[1].text, size=14, anchor="end")
    for index, line in enumerate(note_lines):
        text(20, axis_y + 30 + index * 20, line, size=16)
    elements.extend(["</g>", "</svg>", ""])
    return "\n".join(elements)


if __name__ == "__main__":
    for name in ("screenshot-form", "text-updates"):
        source = CHART_DIRECTORY / f"{name}.html"
        source.with_suffix(".svg").write_text(render_svg(source), encoding="utf-8")
        print(f"Generated {name}.svg")
