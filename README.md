# dry4csharp

## Attribution

`dry4csharp` continues the lineage of Robert C. ("Uncle Bob") Martin's original **dry4clj**, and is
a C# port of its Java sibling **dry4java**.

---

## Overview

`dry4csharp` finds candidate duplicate C# code across files and directories. It reports fuzzy
structural matches by filename and line range so another mechanism can evaluate and reduce
duplication.

`dry4csharp` parses C# source with Roslyn, selects C# declarations as comparison candidates,
normalizes each candidate's syntax tree, and compares sets of structural fingerprints with Jaccard
similarity:

```text
score = shared fingerprints / all fingerprints seen in either candidate
```

Names and literal values normalize away, while C# syntax shape remains. Type declarations (such as
classes, structs, interfaces, records, enums), members (methods, constructors, fields, initializers,
enum members), lambdas, expressions, statements, modifiers, and operators all contribute structural
nodes.

## Usage

```bash
dotnet build -c Release
dotnet run --project src/Dry4CSharp -c Release -- [options] [file-or-directory ...]
```

Options:

```text
--threshold N   Minimum structural similarity score, default 0.82
--min-lines N   Minimum source lines in a candidate declaration, default 4
--min-nodes N   Minimum normalized syntax nodes, default 20
--format F      text or edn, default text
--edn           Same as --format edn
--text          Same as --format text
```

When no paths are provided, dry4csharp scans `src`. Directory arguments recursively include `.cs`
files.

Default text output:

```text
DUPLICATE score=0.89
  src/App/Invoice.cs:12-25
  src/App/Receipt.cs:30-44
```

EDN output:

```clojure
{:candidates
 [{:score 0.8909090909090909
   :left {:file "src/App/Invoice.cs", :start-line 12, :end-line 25}
   :right {:file "src/App/Receipt.cs", :start-line 30, :end-line 44}
   :left-nodes 88
   :right-nodes 91}]}
```

## Development

```bash
dotnet test
```
