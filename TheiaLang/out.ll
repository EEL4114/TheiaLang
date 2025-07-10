; ModuleID = 'theia_module'
declare i32 @puts(i8*, ...)
@.theia_print_str = private constant[19 x i8] c"Hello from Theia!\0A\00"

define i32 @main() {
entry:
  %defaultInt = alloca i32
  %defaultFloat = alloca double
  %defaultBool = alloca i1
  %fg = alloca i1
  store i1 0, i1* %fg
  %ffg = alloca i1
  store i1 1, i1* %ffg
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
  %g = alloca double
  store double 3.0, double* %g
  %h = alloca double
  %tmp3 = load double, double* %f
  %tmp4 = load double, double* %g
  %tmp5 = fsub double %tmp3, %tmp4
  store double %tmp5, double* %h
  %ok = alloca i1
  %tmp6 = load i32, i32* %c
  %tmp7 = icmp sgt i32 %tmp6, 5
  store i1 %tmp7, i1* %ok
  %tmp8 = load double, double* %f
  %tmp9 = fmul double %tmp8, 2.0
  store double %tmp9, double* %f
call i32 @puts(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))
  %tmp10 = load i32, i32* %c
  ret i32 %tmp10
  ret i32 0
}

