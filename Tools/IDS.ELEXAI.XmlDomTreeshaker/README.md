# IDS.ELEXAI.XmlDomTreeshaker

`IDS.ELEXAI.XmlDomTreeshaker` (command name: `shake`) splits XML nodes or attributes into two complementary datasets:

- `train`: the source XML with the sampled nodes removed;
- `eval`: the source XML with the non-sampled nodes removed.

A sampling rule identifies XML nodes or attributes with an XPath expression. 
The configured percentage of the matches is selected for the evaluation dataset, while the remaining matches stay in the training dataset. 
The rest of the XML document is retained in both outputs.

**imporant note:** `shake` only applies the **sampling at the file level**. 
It does not sample across multiple files in the input directory. 
So if you have a dictionary with 1,000 KWIC examples in one XML file, a sampling rate of 5% selects 50 examples from that complete dictionary. 
If the same dictionary is stored as 100 XML files containing 10 KWIC examples each, 5% is calculated for every file. 
Because the result is rounded up, one example is selected from each file, resulting in 100 evaluation examples overall.

## How sampling works

Sampling is performed separately for each XML file in the input directory. For every rule, `shake`:

1. evaluates the XPath expression against the original XML document;
2. randomly selects `sampling_rate` percent of the matches for `eval`;
3. writes the complementary set to `train`.

The number of evaluation items is rounded up:

```text
evaluation item count = ceil(number of matches × sampling_rate / 100)
```

For example, if a file contains 100 examples and `sampling_rate` is `5`, five examples are written to `eval` and 95 remain in `train`.

The file boundary is important. If one XML file contains an entire dictionary with 1,000 KWIC examples, a sampling rate of 5% selects 50 examples from that complete dictionary. If the same dictionary is stored as 100 XML files containing 10 KWIC examples each, 5% is calculated for every file. Because the result is rounded up, one example is selected from each file, resulting in 100 evaluation examples overall. A sampling rate is therefore **not applied globally across all input files**.

A sampling rate of `0` selects no matches for `eval`, while a rate of `100` selects all matches for `eval`.

## Requirements and build

- .NET 8 SDK or later

Build the tool from the repository root:

```powershell
dotnet build Tools\IDS.ELEXAI.XmlDomTreeshaker\IDS.ELEXAI.XmlDomTreeshaker.csproj
```

Run it without publishing:

```powershell
dotnet run --project Tools\IDS.ELEXAI.XmlDomTreeshaker\IDS.ELEXAI.XmlDomTreeshaker.csproj -- <config.json> <input-directory> <output-directory>
```

A published executable uses the shorter command:

```powershell
shake.exe <config.json> <input-directory> <output-directory>
```

On Linux or macOS, invoke the published executable as `./shake`.

## Configuration

The configuration is a JSON object with a random seed and an array of rules:

```json
{
  "seed": "1337021632",
  "rules": [
	{
	  "sampling_rate": 5,
	  "xpath": "//example"
	}
  ]
}
```

### `seed`

The seed controls the random selection.

- Use a numeric string, such as `"1337021632"`, for reproducible results.
- Use `"RANDOM"` for a seed based on the current time and a different, non-reproducible selection.
- A numeric seed must fit in a signed 64-bit integer.

For repeatable output, keep the seed, XPath expressions, XML contents, and input directory ordering unchanged.

### `rules`

`rules` is an array of one or more sampling rules. Every rule has two properties:

| Property | Type | Meaning |
| --- | --- | --- |
| `sampling_rate` | integer | Percentage of matched items assigned to `eval`. Use values from `0` to `100`. |
| `xpath` | string | XPath expression selecting the elements or attributes to sample. |

All rules are evaluated against the original input document. They can target elements or attributes. If a rule matches no items, `shake` writes a warning to the log and continues.

Rules should normally select disjoint items. Overlapping rules—for example, one rule selecting an entry and another selecting examples inside that entry—can cause the parent to be removed in one output even though selected descendants belong to that output.

XPath expressions use the standard .NET XML XPath syntax. Namespace prefixes that cannot be resolved are stripped as a fallback; for namespaced documents, `local-name()` expressions are an explicit and predictable alternative.

## Examples

### Example 1: split 10% of KWIC examples

Given a configuration such as:

```json
{
  "seed": "2026",
  "rules": [
	{
	  "sampling_rate": 10,
	  "xpath": "//entry/sense/example"
	}
  ]
}
```

and an input file:

```xml
<dictionary>
  <entry id="1">
	<sense>
	  <example id="a">First KWIC example</example>
	  <example id="b">Second KWIC example</example>
	  <example id="c">Third KWIC example</example>
	  <example id="d">Fourth KWIC example</example>
	  <example id="e">Fifth KWIC example</example>
	  <example id="f">Sixth KWIC example</example>
	  <example id="g">Seventh KWIC example</example>
	  <example id="h">Eighth KWIC example</example>
	  <example id="i">Ninth KWIC example</example>
	  <example id="j">Tenth KWIC example</example>
	  <!-- more examples -->
	</sense>
  </entry>
</dictionary>
```

run:

```powershell
shake.exe config.json .\data\dictionary .\output
```

Ten percent of the `example` elements in each input XML file are assigned to `eval`. The remaining examples are assigned to `train`. The `dictionary`, `entry`, and `sense` structure remains present in both files unless another rule explicitly selects it.

### Example 2: sample two independent element types

This configuration applies a different hold-out rate to full examples and to quotations:

```json
{
  "seed": "42",
  "rules": [
	{
	  "sampling_rate": 10,
	  "xpath": "//example"
	},
	{
	  "sampling_rate": 20,
	  "xpath": "//quotation"
	}
  ]
}
```

Each XPath is sampled independently. The two expressions should not overlap if the intended result is a clean split of both node types.

### Example 3: remove selected attributes

Rules can also select attributes:

```json
{
  "seed": "123456",
  "rules": [
	{
	  "sampling_rate": 25,
	  "xpath": "//entry/@annotation"
	}
  ]
}
```

In this example, 25% of the `annotation` attributes are retained in `eval` and removed from `train`. The complementary 75% are retained in `train` and removed from `eval`. Elements that own those attributes remain in both outputs.

### Example 4: handle an XML namespace

For a document using the default namespace:

```xml
<dictionary xmlns="urn:example:dictionary">
  <entry>
	<example>Example text</example>
  </entry>
</dictionary>
```

use `local-name()` to select namespaced elements:

```json
{
  "seed": "1337021632",
  "rules": [
	{
	  "sampling_rate": 5,
	  "xpath": "//*[local-name()='example']"
	}
  ]
}
```

### Example 5: random, non-reproducible hold-out

```json
{
  "seed": "RANDOM",
  "rules": [
	{
	  "sampling_rate": 15,
	  "xpath": "//sense/example"
	}
  ]
}
```

This selects a fresh 15% evaluation sample on each run. Use this only when reproducibility is not required.

## Create a configuration template

Running `shake init` creates `template.json` in the current directory:

```powershell
shake.exe init
```

The generated file contains the default seed and a rule with a 20% sampling rate and the broad XPath expression `//*`. Replace `//*` with the XPath for the specific elements or attributes that should be sampled before using the configuration. Selecting `//*` includes the document element and all descendant elements, which is usually not the desired dictionary-level split.

## Output structure

`shake` reads all files directly inside the input directory. Subdirectories are not traversed. It preserves each source filename and produces:

```text
output/
├── eval/
│   └── source.xml
├── train/
│   └── source.xml
└── log.txt
```

- `eval/<file>` contains only the sampled matches for each rule, with all unrelated XML content retained.
- `train/<file>` contains the complementary matches for each rule, with all unrelated XML content retained.
- `log.txt` records processed files and warnings, and is also mirrored to the console.

The output root, `eval`, and `train` directories are created when needed. `log.txt` and output files with matching names are overwritten on each run.

If an input file cannot be parsed as XML, the unchanged copy already written to the corresponding output directories is kept and a warning is logged.

## Command reference

```text
shake <config.json> <input-directory> <output-directory>
shake init
shake help
```
