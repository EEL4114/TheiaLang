## 3 Booleans

### 3.1 Values
Boolean values are `true` and `false` (see §1.1).

### 3.2 Representation
Boolean values take up one Byte (8 bits) of memory; they are represented in the
following way:
* `false`: 0b00000000 (0x00)
* `true`: 0b00000001 (0x01)

### 3.3 Operators

#### 3.3.1 Unary
Booleans can be negated (inverted) using `!`, returning another `bool`;

``` C#
bool isDay;
printn("%isDay");               // -> false
isDay = !isDay;
printn("%isDay");               // -> true
```
image = u8(f16(image[.., ..]) *<8, 8> matrix);
#### 3.3.2 Binary
The following operations are valid between two `bool` values, and return a
`bool`: 
* `&&` (AND)*
* `||` (OR)*
* `&` (bitwise AND)
* `|` (bitwise OR)
* `^` (bitwise XOR)
* `==` (equality)
* `!=` (inequality).

\* - exclusive to booleans

`&&` and `||` operate in a 'short-circuit'-manner:
* for `&&` when the 1st operand is `false`, the 2nd operand is not evaluated, because we know the result is `false`.  
* for `||` when the 1st operand is `true`, the 2nd operand is not evaluated, because we know the result is `false`.

> Note that for booleans, `&`/`|` behave the same as `&&`/`||`, just without the 'short-circuiting'.
For all of the bitwise operations (`&`, `|`, `^`), *compound assignment* (see § 2.4) is supported:

``` C#
bool a;
bool b;

a = !b;
printn("%a");                   // -> true

bool c = a && b;
printn(%c)                      // -> false
c = a & b;
printn(%c)                      // -> false

c = a || b;
printn(%c)                      // -> true
c = a | b;
printn(%c)                      // -> true

c = a ^ b;
printn(%c)                      // -> true

c = a == b;
printn(%c)                      // -> false

c = a != b;
printn(%c)                      // -> true

// compound assignment
c &= b;                         // equivalent to: `c = c & b;`
printn(%c)                      // -> false
c |= b;                         // equivalent to: `c = c | b;`
printn(%c)                      // -> true
c ^= b;                         // equivalent to: `c = c ^ b;`
printn(%c)                      // -> true

```

### 3.3.3
The equality operator `==` can compare two values a and b of the same (or
implicitly castable) type, returning a `bool`:
* `true`, if a and b have the same value
* `false`, otherwise

### 3.4 Casting (Temporary)
Booleans cannot be implicitly cast from or to any type; the following explicit
casts exist (also see: §??? Casting):
* integers (any subvariant) -> `bool`:
    * `0` -> `false`
    * other -> `true`
* floating-point numbers (any subvariant) -> `bool`:
    * `0` -> `false`
    * other -> `true`
* `string` -> `bool`:
    * `"false"`, `"False"`, `"FALSE"` -> `false`
    * `"true"`, `"True"`, `"TRUE"` -> `true`