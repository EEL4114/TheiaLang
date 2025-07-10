## 2 Constants, Variables

### 2.1 Constants

Often, you will find yourself needing some specific value (e.g. standard gravity:
~9.81 m/s²) when doing calculations. You would like
that value to be available in all the places where you need them without
needing to re-define them while also not being able to accidentally change it.

All constants are defined trough: `const (<Type>) <name> (= <value>);`, where:
* the parenthesis `(`, `)` contains optional parts
* `const` specifies that the assigned value cannot be mutated at runtime
* `<name>` is a placeholder for the name assigned to the field (e.g. `PI`);\
* `<Type>` is a placeholder for the type the field has (e.g. `bool`, `f32`, etc.)
* `=`, explicitly specifies that the field gets initialised with a custom value
* `<value>` is a placeholder for the assigned value (e.g. `true`, `17`, `"Hi!"`)
* `;` to terminate the statement

> Note that either Type or value must be assigned for the compiler to be able to infer the other.

You can introduce a named *Constant*, by using the `const` modifier:
``` C#
const f32 STANDARD_GRAVITY = 9.80665;   // (1) in m/s²
const M_EARTH = 5.97219e24;             // (2) in kg - type inferred
const f32 M_MARS;                       // (3) in kg - this will be 0!
``` 

By Convention, constants are UPPER_CASE.
As can be seen in (2), the type of a const can be implicit as well.

### 2.2 Variables

Now, while you may want some constants in your program, you will want to have
a lot of values that can change at runtime: Variables. You do not need any
`var` or `mut` for this, any value you declare is variable by default.

Variables are declared in an similar fashion to constants (see $2.1), just without
the `const` modifier:  `(<Type>) <name> (= <value>);`

Variables can be mutated like this: `<name> = <expression>`, where:
* `<name>` is a placeholder for the name assigned to the variable;
* `=` signals that we want to assign a value to that variable
* `<expression>` is a placeholder for any expression that evaluates to something of the same (or implicitly castable) type as the variable
* `;` to terminate the statement

By convention, variables are `camelCase`.

#### Example: Declaring and Mutating values
``` C#
u31 GLOBAL_CONST = 17;            //
f32 TAU = 2 * f32.PI;             // multiple of another const
ATAN_5 = #run Math.Atan(5);       // implicit type, compile-time evaluation
u8 globalVar = 56;

void Main()
{
    // explicit declaration and initialisation
    int count = 3;
    float mass = 56.8;
    bool isDone = false;
    string name = "Stone";

    // explicit type declaration, implicit initialisation with default values
    int defaultCount;
    printn(%defaultCount);        // -> 0
    float defaultMass;
    printn(%defaultMass);         // -> 0
    bool defaultDone;
    printn(%defaultDone)          // -> false
    string defaultName;
    printn(%defaultName)          // -> ""

    // use %symbol% if you don't want a space after the symbol value:
    printn("I have %count %name%s with a total mass of %mass kg.");
    // -> "I have 3 Stones with a total mass of 56.8 kg."

    // changing values
    count = 3455;
    printn(count);                // -> 3455
    mass = 7.3;
    printn(%mass);                // -> 7.3
    isDone = true;
    printn(&mass);                // -> true

    // uninitialised variables -> "random" value
    int!? riskyCount = ---;
    print(%riskyCount);           // -> 789076543
    float !? mass2 = ---;         // -> 0
}
```

Variables, similar to constants get defined like this:\
`type name = value;`\
`f128 mass = 9e765;`

For every type, there is a default value. If you don't need a variable to
initialise with any specific value, you can just leave the value assignment:\
`u32 count;` *-> defaults to 0*

A variable can often also just be declared through its value, skipping the
type declaration:\
`isInitialised = false;` *-> bool*\
For the sake of clarity, this will typically be avoided here.

Note that type annotations are not necessary for string interpolation.
However, if you want any specific formatting, you will need to specify that
manually.

### 2.3 Multiple Assignment

You can declare (and assign) multiple variables of the same type on a single line
like this: `<Type>`

``` C#
// declaration and assignment on same line:
u32 a; a = 3;
printn("%a");                     // 3
// multiple declaration and assignment
u32 b, c, d = 7;
u32 e, f = 8, 9;
printn("%b %c %d %e, %f")         // 7 7 7 8 9
// you can't do this: u32 g = h = 5;
// inference still works
i, j = 3.5;
printn("%typeOf(i)")              // f32
// this also includes values computed at compile time
k, l = #run(4 * 8), #run(87654333 / 7890);
```

### 2.4 Compound Assignment

Some operators support *compound assignment*: an operation and an assignment through one operator: `<identifier>` `<op>` `=` `<expression>;`, where:
* `<identifier>` is the identifier of a valid, assignable variable
* `<op>` is an operator that supports compound assignment (e.g. `+`, `*`, etc.) 
* `=` specifies the assignment
* `<expression>` is any expression that evaluates to something of the same (or implicitly castable) type as the variable
* `;` to terminate the statement

> Note that operators that do support compound assignment are explicitly stated to do so when they're introduced for the respective types.

#### 2.4.1 Execution
The compound assignment `<x>` `<op>` `=` `<y>;`, unless explicitly annotated with the `reorder` modifier, to evaluating `x` once, `y` once, then assigning `x = x op y`.

> Note that the compiler will still reorder those when it can prove that it does not change the semantics; If it does, you must decide between adding the `strict` (see § ???; left -> right sequencing) or `reorder` (see § ???; allow reordering even if it changes semantics) annotation or block.

A compound assignment is equivalent to only evaluating the left-hand side exactly once, then the right-hand side unless using the `reorder` modifier. This makes equivalence to a normal operation and assignment non-trivial the left-hand side expression has side effects, as those will also only happen once.
 
#### 2.4.2 Examples

``` C#
// trivial
u32 a, b = 5;
a = a + 6;                      // operation and assignment
b += 6;                         // compound assignment
printn("%a");                     // -> 11
printn("%b");                     // -> 11

// non-trivial

u32[20] intArray;               // implicitly zero-initialised
u32 i = 1;
strict intArray[i++] += i;
printn("The first element of the array is %intArray[1]");
                                // -> The first element of the array is 2
printn("i is %i");              // -> i is 2

intArray[1] = 0;                // reset to initial state
i = 1;

// not equivalent (multiple increment)
reorder intArray[i++] = intArray[i++] + i;
                                // this needs to be marked unsafe since it uses
                                // the symbol that gets post-incremented thrice
printn("The first element of the array is %intArray[1]");
                                // -> The first element of the array is 1
printn("i is %i");              // -> i is 3

intArray[1] = 0;                // reset to initial state
i = 1;

// equivalent (single increment)
strict intArray[i++] = intArray[i] + i;
printn("The first element of the array is %intArray[1]");
                                // -> The first element of the array is 1
printn("i is %i");              // -> i is 1
                                // this is since the LHS gets evaluated first,
                                // incrementing `i` to 2, then its value gets
                                // assigned 
```

### 2.5 Swapping Variables
``` C#
u32 a, b = 4, 7;
printn("a is %a; b is %b");       // -> a is 4; b is 7
a, b = b, a;
printn("a is %a; b is %b");       // -> a is 7; b is 4
```
