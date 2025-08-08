; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"


define i32 @main() {
entry:
  %a_main = alloca i32
  store i32 17, i32* %a_main
  %tmp13 = load i32, i32* %a_main
  %tmp14 = sub i32 %tmp13, 17

  ret i32 %tmp14
}

