import random

def generate_struct(i, with_methods):
    name = f"Struct{i}"
    fields = ", ".join(f"s32 field{j}" for j in range(5))
    result = [f"struct {name}({fields})"]
    if with_methods:
        result.append("{")
        for m in range(2):
            result.append(f"    s32 method{m}() {{")
            result.append(f"        s32 x = {random.randint(1, 100)};")
            result.append("        return x;")
            result.append("    }")
        result.append("}")
    else:
        result[-1] += ";"
    return "\n".join(result)

def generate_function(i):
    result = [f"s32 func{i}() {{"]

    for v in range(10):
        val = random.randint(1, 1000)
        result.append(f"    s32 var{v} = {val};")

    for v in range(5):
        a = random.randint(1, 10)
        b = random.randint(1, 10)
        result.append(f"    var{v} = var{a % 10} + var{b % 10};")

    result.append("    return var0;")
    result.append("}")
    return "\n".join(result)

def main():
    total_lines = 0
    structs = 4500
    funcs = 4000

    with open("Example2.tia", "w") as f:
        for i in range(structs):
            block = generate_struct(i, with_methods=(i % 3 == 0))
            f.write(block + "\n\n")
            total_lines += block.count("\n") + 2

        for i in range(funcs):
            block = generate_function(i)
            f.write(block + "\n\n")
            total_lines += block.count("\n") + 2

        # Write a main function
        f.write("s32 main() {\n")
        for i in range(funcs):
            f.write(f"    func{i}();\n")
        f.write("    return 0;\n}\n")

    print(f"Generated ~{total_lines} lines of code in 'Generated.tia'")

if __name__ == "__main__":
    main()
