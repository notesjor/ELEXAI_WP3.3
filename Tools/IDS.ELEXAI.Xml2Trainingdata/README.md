# IDS.ELEXAI.Xml2Trainingdata

`IDS.ELEXAI.Xml2Trainingdata` (command name: `xml2td`) converts XML files into chat-style JSON training examples. 
A JSON template defines the system, user, and assistant messages, while XPath rules extract values from each input XML document
and insert them into placeholders in the message content.

For every successfully parsed XML file, `xml2td` creates one JSON file in the output directory. 
The tool does not modify the source XML files.

## How conversion works

Conversion is performed separately for every XML file in the input directory. For each file, `xml2td`:

1. loads the XML document;
2. evaluates each XPath rule in the message template;
3. replaces matching placeholders with the selected XML values;
4. writes the resulting `messages` array as JSON.

Static message content is copied unchanged. A template placeholder consists of a rule name and optional selector and `text` modifier:

```text
{rule}
{rule[index]}
{rule[all]}
{rule.text}
{rule[index].text}
{rule[all].text}
```

Without an explicit index, the first XPath match is used. The `all` selector inserts every match, separated by a newline. 
Without `.text`, an element is represented by its inner XML; with `.text`, its text content is used. 
Attributes return their attribute value in either form.

## Requirements and build

- .NET 8 SDK or later

Build the tool from the repository root:

```powershell
dotnet build Tools\IDS.ELEXAI.Xml2Trainingdata\IDS.ELEXAI.Xml2Trainingdata.csproj
```

Run it without publishing:

```powershell
dotnet run --project Tools\IDS.ELEXAI.Xml2Trainingdata\IDS.ELEXAI.Xml2Trainingdata.csproj -- <template.json> <input-directory> <output-directory>
```

A published executable uses the shorter command:

```powershell
xml2td.exe <template.json> <input-directory> <output-directory>
```

On Linux or macOS, invoke the published executable as `./xml2td`.

## Template configuration

A template is a JSON object containing a `messages` array:

```json
{
  "messages": [
	{
	  "role": "system",
	  "content": "You are an experienced lexicographer."
	},
	{
	  "role": "user",
	  "content": "Here are several citations:\n{citation[all].text}\nWrite a definition for them.",
	  "rules": [
		{
		  "name": "citation",
		  "xpath": "//sense[cit]/cit/quote"
		}
	  ]
	},
	{
	  "role": "assistant",
	  "content": "{definition.text}",
	  "rules": [
		{
		  "name": "definition",
		  "xpath": "//sense[cit]/def"
		}
	  ]
	}
  ]
}
```

### `messages`

Each message has a role and content:

| Property | Type | Meaning |
| --- | --- | --- |
| `role` | string | Chat role. Supported values are `system`, `user`, and `assistant`. |
| `content` | string | Static text or a template containing one or more placeholders. |
| `rules` | array | Rules used to resolve placeholders in this message. This property is optional. |

Messages are written in the same order as they appear in the template.

### `rules`

Each rule connects a placeholder name to an XPath expression:

| Property | Type | Meaning |
| --- | --- | --- |
| `name` | string | Name referenced inside placeholders, for example `citation` in `{citation[all].text}`. |
| `xpath` | string | XPath expression selecting elements or attributes from the XML document(s). |
| `lookup` | string | Optional directory whose XML files are searched instead of the current document. |

Rule names must match placeholders exactly. A rule name may contain letters, digits, underscores, dots, and hyphens, but must start with a letter or underscore.

Without `lookup`, a rule keeps the existing behavior and searches only the current XML document. With `lookup`, the XPath is evaluated separately in every `.xml` file directly inside that directory, excluding the current source file, and the matches are combined. Files are processed in filename order so that numeric selectors and `{rule[all]}` are deterministic. Relative lookup paths are resolved relative to the current source file's directory. Subdirectories are not traversed.

If an indexed expression selects no node at that position, it is replaced with an empty string. Placeholders that do not have a corresponding rule remain unchanged. If an XPath returns no nodes, `all` produces an empty string.

XPath expressions use standard .NET XML XPath syntax. Namespace prefixes that cannot be resolved are stripped as a fallback. For namespaced documents, `local-name()` expressions are an explicit alternative.

## Placeholder reference

Given a rule named `citation` with three XML matches, placeholders behave as follows:

| Placeholder | Result |
| --- | --- |
| `{citation}` | Inner XML of the first match. |
| `{citation.text}` | Text content of the first match. |
| `{citation[1]}` | Inner XML of the second match. Zero-based indexing is used. |
| `{citation[1].text}` | Text content of the second match. |
| `{citation[all]}` | Inner XML of every match, separated by newlines. |
| `{citation[all].text}` | Text content of every match, separated by newlines. |

For example, given:

```xml
<cit><quote>First <term>useful</term> citation</quote></cit>
```

- `{citation.text}` produces `First useful citation`.
- `{citation}` produces `First <term>useful</term> citation`.

## Examples

### Example 1: create definition prompts from citations

Given this template:

```json
{
  "messages": [
	{
	  "role": "user",
	  "content": "Use these citations:\n{citation[all].text}\nWrite one definition.",
	  "rules": [
		{
		  "name": "citation",
		  "xpath": "//sense/cit/quote"
		}
	  ]
	},
	{
	  "role": "assistant",
	  "content": "{definition.text}",
	  "rules": [
		{
		  "name": "definition",
		  "xpath": "//sense/def"
		}
	  ]
	}
  ]
}
```

and an input XML file:

```xml
<entry>
  <sense>
	<cit><quote>First citation text.</quote></cit>
	<cit><quote>Second citation text.</quote></cit>
	<def>A concise definition.</def>
  </sense>
</entry>
```

run:

```powershell
xml2td.exe template.json .\data\xml .\output
```

The generated user message contains both quotations, one per line, and the assistant message contains the definition.

### Example 2: select a specific occurrence

XPath returns results in document order. Use a zero-based selector to choose one occurrence:

```json
{
  "messages": [
	{
	  "role": "user",
	  "content": "Primary citation: {citation[0].text}\nAlternative citation: {citation[1].text}",
	  "rules": [
		{
		  "name": "citation",
		  "xpath": "//quote"
		}
	  ]
	}
  ]
}
```

### Example 3: preserve nested XML markup

Omit `.text` to insert the inner XML rather than flattened text:

```json
{
  "messages": [
	{
	  "role": "user",
	  "content": "Preserve the following annotation:\n{example}",
	  "rules": [
		{
		  "name": "example",
		  "xpath": "//example"
		}
	  ]
	}
  ]
}
```

### Example 4: extract XML attributes

Rules can select attributes:

```xml
<entry id="lemma-42">...</entry>
```

```json
{
  "messages": [
	{
	  "role": "user",
	  "content": "Create training data for entry {entryId}.",
	  "rules": [
		{
		  "name": "entryId",
		  "xpath": "//entry/@id"
		}
	  ]
	}
  ]
}
```

Attribute rules return the attribute value without the surrounding attribute syntax.

### Example 5: handle an XML namespace

For a document using a default namespace:

```xml
<dictionary xmlns="urn:example:dictionary">
  <entry>
	<definition>Definition text</definition>
  </entry>
</dictionary>
```

use `local-name()` in the XPath expression:

```json
{
  "messages": [
	{
	  "role": "assistant",
	  "content": "{definition.text}",
	  "rules": [
		{
		  "name": "definition",
		  "xpath": "//*[local-name()='definition']"
		}
	  ]
	}
  ]
}
```

## Create a template

Running `xml2td init` creates `template.json` in the current directory:

```powershell
xml2td.exe init
```

The generated template contains system, user, and assistant messages for a citation-to-definition example. Replace the messages and XPath expressions with the structure required by the intended training dataset.

## Output structure

`xml2td` reads files directly inside the input directory. Subdirectories are not traversed. Each source filename is retained and receives a `.json` suffix:

```text
output/
├── prompts/
│   ├── source.xml.json
│   └── another.xml.json
└── log.txt
```

Each prompt file contains one JSON object:

```json
{
  "messages": [
	{
	  "role": "user",
	  "content": "Resolved user message"
	},
	{
	  "role": "assistant",
	  "content": "Resolved assistant message"
	}
  ]
}
```

The output root, `prompts` directory, and `log.txt` are created when needed. Existing `log.txt` files are replaced on each run, and generated prompt files with matching names are overwritten.

If an input file cannot be parsed as XML, no prompt is written for that file and a warning is logged. Log messages are mirrored to the console.

## Command reference

```text
xml2td <template.json> <input-directory> <output-directory>
xml2td init
xml2td help
```
