# Mango
Mango is basically JSON, but with more features.
## Syntax
Mango syntax is very similar to JSON. The main differences are described below.
### Nil
The keyword `nil`can be used as well as `null`.
### Quotes
Keys do not require quotes if the key only contains letters, digits, and underscores. If the key only contains digits, it must be surrounded by quotes.
```
{
  x: 3
  2x: 6
  "23": 23
}
```
### Commas
Commas are optional.
```
[1 2, 3]
```
### Comments
Comments can be added to Mango data. Single line comments start with `--`. Multiline comments start with `--` followed by one or more `/` characters, and are closed by the same number of `/` characters followed by `--`.
```
{
  -- A single line comment
  x: 3

  --/
    A multi line comment
  /--

  --///
    A multi line comment using more / characters
  ///--
}
```
### Variables
Variable names are prefixed with `$` and can only contain letters, digits, and underscores. Variables can be declared at the begining of a list or an object. The variables can then be used inside the list or object they were declared in. If a variable is used without being declared it will have a value of `nil`.
```
{
  $var = 5

  x: $var
  y: $var
}
```
### Templates
Template names are prefixed with `#` and can only contain letters, digits, and underscores. Templates can be declared at the begining of a list or an object. The templates can then be used inside the list or object they were declared in. Templates can be declared with any number of parameters. Arguments can then be passed to the template. Parameters have a default value of `nil`. If a template is used without being declared it will have a value of `nil`.
```
{
  -- Template declaration
  #template[$a $b] = {
    x: $a
    y: $b
  }

  -- This will result in a value of: {x: 3 y: 6}
  value: #template[3 6]
}
```
### Unpacking
Variables and templates can be unpacked into a list or an object.
```
{
  $v = {
    a: 1
    b: 2
  }

  #t[$x] = [1 $x 3]

  -- This will result in: {a: 1 b: 2 c: 3}
  obj: {
    -- The contents of $v will be inserted into the object
    $$v
    c: 3
  }

  -- This will result in: [0 1 2 3 4]
  list: [
    0
    -- The contents of the template will be inserted into the list
    ##t[2]
    4
  ]
}
```
### String Interpolation
Mango values can be inserted into strings by placing the value between `\{` and `}` in a string literal.
```
{
  $thing = "World"

  text: "Hello \{$thing}!"
}
```
