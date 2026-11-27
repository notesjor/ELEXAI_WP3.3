import os
import sys

try:
    from lxml import etree as ET
except ImportError:
    print("missing dependency 'lxml'. Install it with: pip install lxml")
    sys.exit(1)


def namespace_insensitive_xpath(xpath_expr):
    xpath_expr = xpath_expr.strip()

    if "//" in xpath_expr:
        prefix = xpath_expr.split("//", 1)[0]
        remainder = xpath_expr.split("//", 1)[1]
        if prefix or "[" in remainder or "@" in remainder or "*" in remainder:
            return xpath_expr
        segments = remainder.strip("/").split("/")
        if any(not segment or "[" in segment or "*" in segment for segment in segments):
            return xpath_expr
        first = "*" if segments[0] == "*" else f"*[local-name()='{segments[0]}']"
        rest = "".join(
            "/*" if segment == "*" else f"/*[local-name()='{segment}']"
            for segment in segments[1:]
        )
        return f".//{first}{rest}"

    if xpath_expr.startswith("/") and "[" not in xpath_expr and "@" not in xpath_expr:
        segments = xpath_expr.strip("/").split("/")
        if all(segment and "[" not in segment for segment in segments):
            return "/" + "/".join(
                "*" if segment == "*" else f"*[local-name()='{segment}']"
                for segment in segments
            )

    return xpath_expr


def split_xml(xml_file, xpath_expr, output_dir):
    if not os.path.isfile(xml_file):
        print(f"file '{xml_file}' not found.")
        sys.exit(1)

    os.makedirs(output_dir, exist_ok=True)

    try:
        tree = ET.parse(xml_file)
        root = tree.getroot()
    except ET.XMLSyntaxError as e:
        print(f"xml parsing error: {e}")
        sys.exit(1)

    xpath_expr = namespace_insensitive_xpath(xpath_expr)

    try:
        nodes = root.xpath(xpath_expr)
    except ET.XPathEvalError as e:
        print(f"invalid xpath: {e}")
        sys.exit(1)

    if not nodes:
        print("the xpath didn't match any xml-node.")
        return

    print(f"{len(nodes)} entries found. extracting them to: '{output_dir}'...")

    for index, node in enumerate(nodes, start=1):
        new_tree = ET.ElementTree(node)        
        ET.indent(new_tree, space="  ", level=0)      
        output_filename = os.path.join(output_dir, f"entry_{index:03d}.xml")
        new_tree.write(output_filename, encoding="utf-8", xml_declaration=True)

    print("Fertig!")

if __name__ == "__main__":
    if len(sys.argv) != 4:
        print("usage: python split_xml.py <xml_file> <xpath> <output-folder>")
        print('example:   python split_xml.py dictionary.xml "//entry" ./output')
        sys.exit(1)

    xml_input = sys.argv[1]
    xpath_query = sys.argv[2]
    out_folder = sys.argv[3]

    split_xml(xml_input, xpath_query, out_folder)