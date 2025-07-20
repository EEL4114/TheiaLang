## 1 Types, Print

For making proper use of data, we have to use `types`, that way we can
process data more efficiently and we can also verify if the data we get is
the kind of data we expect to work with. The fundamental data types are
called *literals*. Literals are constant at compile time.

The following literal types exist:
* *booleans* - truth values: true, false
* *integers* - whole numbers: 0, 42, -7
* *floats*   - decimal numbers: 3.1415, 2.64e13
* *strings*  - "Hello", "こんにちは", "42"

A type specifies how a value looks in data and what operations can be 
performed on values of that type, e.g. `+`.
Types can be used in constants and variables; a type defines how much memory
(in bytes; 1 byte == 8 bits) is needed to store it.

Types, Values and Variables, together with operators form *expressions*, e.g.
`(a + 7) * b`. An expression evaluates to a value of some type again, so we
can assign it to a variable through a 'statement' : `c = (a + 7) * b;` 
Note that every statement ends with a semicolon `;`

When working with different types, there will be an *cast* if one type can be
converted into another one *without loss of data*.  Otherwise, a manual cast
has to happen, e.g. converting `"42"` into an `int`. If a type conversion is
not possible (e.g. converting `"Hi"` into an `int`), there will be a *compile
error*.

Types can sometimes be *inferred* (e.g. `a = 17;`) though often you also want
or need to specify types manually. Note that even when a type is not explicitly
specified, it will always be constant at compile time and can be inspected
through a static analyser.

### 1.1 Primitive Types

#### **Booleans**
*(see §3 "Booleans")*\
Often used for control flow, conditions usually evaluate to a `bool`.

|   name   | size (B) |      values     |
|----------|----------|-----------------|
|   bool   |    1     | `false`, `true` |

Default: `false`

> Note: `bool` can *explicitly* be cast to integer-types and vice versa.

#### **Integers**

Used for representing whole numbers, both *signed* and *unsigned* types exist
with a variety of value ranges:

|   Name   |  Alias   | Size (B) |   Min    |   Max    |
|----------|----------|----------|----------|----------|
|   `u7`   |   ---    |    1     |    0     |   127    |
|   `u8`   |  `byte`  |    1     |    0     |   255    |
|   `s8`   | `sbyte`  |    1     |  -128    |   127    |
|          |          |          |          |          |
|  `u15`   |   ---    |    2     |    0     |  32767   |
|  `u16`   | `ushort` |    2     |    0     |  65535   |
|  `s16`   |  `short` |    2     |  -32768  |  32767   |
|          |          |          |          |          |
|  `u31`   |   ---    |    4     |    0     | 2^31 - 1 |
|  `u32`   |  `uint`  |    4     |    0     | 2^32 - 1 |
|  `s32`   |  `int`   |    4     |  -2^31   | 2^31 - 1 |
|          |          |          |          |          |
|  `u63`   |   ---    |    8     |    0     | 2^63 - 1 |
|  `u64`   | `ulong`  |    8     |    0     | 2^64 - 1 |
|  `s64`   |  `long`  |    8     |  -2^63   | 2^63 - 1 |
|          |          |          |          |          |
|  `u127`  |   ---    |    16    |    0     | 2^127 - 1|
|  `u128`  |   ---    |    16    |    0     | 2^128 - 1|
|  `s128`  |   ---    |    16    |  -2^127  | 2^127 - 1|
|          |          |          |          |          |
|  `u255`  |   ---    |    32    |    0     | 2^255 - 1|
|  `u256`  |   ---    |    32    |    0     | 2^256 - 1|
|  `s256`  |   ---    |    32    |  -2^255  | 2^255 - 1|

Default (all types): `0`.\
Default type for implicitly typed variables: `s32`.

> Note that the types `u7`, `u15`, `u31`, `u63`, `u127` and `u255` exist for representing non-negative integers in a way that keeps them them implicitly convertable to `s8`, `s16`, `s32`, `s64`, `s128` and `s256` and are used for example to indicate the size of a collection - they still own a sign bit but
> * ignore sign bit for all logic
> * initialise with 0 as sign bit
> * cast to larger types by zero-ing the sign (even implictily)
> * preserve sign bit in same-type operations (potentially useful if one needs a bitflag somewhere - still need to be somewhat careful however)

Declaration:
* as regular base-10 number: `42`
* as base-2 number: `0b101010`
* as hexadecimal number: `0x2A`

It is possible to group digits with underscores `_`, for example:
`65_445_637`, `0b1010_0100_1010_1010`, `0xC2_3B_21` for easier readability.
> Note that in hexadecimal form, both capital case and lower case letters are accepted.

#### **Floats**

All binary floating point formats specified by IEEE 754 are available:

|   Name   |  Alias   |  Alias 2  | Size (B) |     Min > 0    |      Max     |
|----------|----------|-----------|----------|----------------|--------------|
|  `f16`   |  `half`  |    ---    |    2     | 6.10×10^−5     | 65504        |
|  `f32`   | `single` |  `float`  |    4     | 1.18×10^−38    | 3.40×10^38   |
|  `f64`   | `double` |    ---    |    8     | 2.23×10^−30    | 1.80×10^308  |
|  `f128`  |   ---    |    ---    |    16    | 3.36×10^−4932  | 1.19×10^4932 |

Default: `0`.\
Default type for implicitly typed variables: `f32`.

> Note that for explicitly stating a number to be a floating point value, you
use the `f` suffix: `a = 4f`. The compiler should still be able to infer types
correctly as long as the variable the value gets assigned to is explicitly typed.

Declaration:
* as regular base-10 decimal: `3.141592654`
* in exponential form, base 10: `2.54e32`
* in binary form, according to IEEE 754: `0b01000000010010010000111111011011`
* in hexadecimal form, according to IEEE 754: `0x40490fdb`

> Note that in hexadecimal form, specifiying type with the `f` suffix is not possible, use the prefix `0h` instead: `0h40490fdb`.
> Digit grouping still is possible just like with integers: `0h40_49_0f_db`

#### **Strings**

Declared using `""`, e.g.: `"42"`, `"Hii"`, `"true"`, `"0x101010"`,
alternatively using `''`, in case you want to use `"` in a string: 
`'Mom said "Hi!"'`. 

String composition:
* struct size: 16 bytes
* `data`: UTF-8 characters, 1-4 bytes per character
* bit0: short string flag
* `data` < 15 bytes:
  * byte 0: `u7` size *note that since this is a `u7`, it doesn't interfere 
    with the short string bitflag*
  * bytes 1-14: `data`
  * byte 15: `zero`
* `data` > 14 bytes:
  * bytes 0-3: `u31` size *note that since this is a `u31`, it doesn'
    interfere with the short string bitflag*
  * bytes 4-7: `u31` capacity
  * bytes 8 - 15: heap pointer (note that we always use 64-bit pointers for
    consistency)
* `data` constant: inline aggressively without metadata

since string length is `u7` / `u31`, it can be implicitly cast to `u32` while
also enforcing non-negative length

### 1.2 Displaying values in the console

Both in your actual code and in the following examples we need to be able to
provide basic console output. In Theia, that is what the `print` and `printn`
functions are for. We will introduce the basics here for illustration purposes;
further details on functions can be found in later chapters, for now, it is
enough to know how to call them and that they expect a `string` input.

``` C#
// basic print function
print("Good");
print("Morning!");  // -> GoodMorning
// only with '\n' a new line gets generated:
print("Good\n");
print("Morning!\n") // -> Good
                    // -> Morning!

// printn automatically adds a '\n' at the end:
printn("Good");
printn("Morning!"); // -> Good
                    // -> Morning!
                    // -> 
                    // (in all following examples we will omit trailing newlines)

printn(42);         // Error: Type mismatch: Type expected: string; Type received: u32.
// `%` can be used to interpolate any symbol
// this works both inside or outside a string
printn("42")        // -> 42
// we can use `%` to interpolate the following expression until a seaparator / end of string
i = 27;             // implicit s32
printn("It's June, 19%i");
                    // -> It's June, 1927
printn("It's June, 19%i .");
                    // -> It's June, 1927 .
// this is ugly, instead, explicitly terminate the symbol name:
printn("It's June, 19%i%.");
                    // -> It's June, 1927.

// ADVANCED: edge cases
// what happens here?
// first `%` opens interpolation scope, second `%` closes it;
// `i` interpreted as text:
printn("%i%i");     // -> 27i
// first `%` opens interpolation scope, second `%` closes it, 
// third `%` opens interpolation scope`i` interpreted as integer:
printn("%i%%i");     // -> 2727
// first `%` opens interpolation scope, ` ` closes it, 
// second `%` opens interpolation scope`i` interpreted as integer:
printn("%i %i");     // -> 27 27

// use curly braces if you want to interpolate more complex expressions:
printn("%42 + 1"):    // -> 42 + 1.
printn("%{42 + 1}");  // -> 43
```

> Note that the compiler will try to evaluate the expression after `%` to a string, in order for it to know when the symbol declaration ends, it needs a separator (space, newline, `%`, end of string) after the symbol name. You can use `{` `}` if you need any of the separators inside the expression you want to interpolate