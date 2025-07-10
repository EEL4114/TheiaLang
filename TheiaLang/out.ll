; ModuleID = 'theia_module'
declare i32 @printf(i8*, ...)

define i32 @main() {
entry:
  %a = alloca i32
  store i32 5, i32* %a
  %b = alloca i32
  store i32 10, i32* %b
  %c = alloca i32
  %tmp0 = load i32, i32* %a
  %tmp1 = load i32, i32* %b
  %tmp2 = add i32 %tmp0, %tmp1
  store i32 %tmp2, i32* %c
  %f = alloca double
  store double 2.5, double* %f
  %ok = alloca i1
  %tmp3 = load i32, i32* %c
  %tmp4 = icmp sgt i32 %tmp3, 5
  store i1 %tmp4, i1* %ok
  %tmp5 = load double, double* %f
  %tmp6 = fmul double %tmp5, 2.0
  store double %tmp6, double* %f
  %tmp7 = load i32, i32* %c
  ret i32 %tmp7
}

