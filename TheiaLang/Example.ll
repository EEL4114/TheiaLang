; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"
declare i32 @puts(i8*, ...)
@.theia_print_str = private constant[19 x i8] c"Hello from Theia!\0A\00"

%Vector3 = type { double, double, double }
%Entity = type { double }

define i32 @Entity.AddHealth(%Entity* %this, double %amount) {
entry:
  %tmp0 = alloca double
  store double %amount, double* %tmp0
  store double 0.0, double* %tmp0
  call i32 @puts(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))
  ret i32 0
}

define i32 @m() {
entry:
  %i = alloca i32
  store i32 3, i32* %i

  %health = alloca i32
  store i32 7, i32* %health

  call i32 @puts(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))
  ret i32 0
}

define i32 @main() {
entry:
  %defaultInt = alloca i32
  %defaultFloat = alloca double
  %defaultBool = alloca i1
  %default_s32 = alloca i32
  %default_f32 = alloca double
  %default_bool = alloca i1
  %i = alloca i32
  %tmp1 = sub i32 0, 1
  store i32 %tmp1, i32* %i

  %j = alloca double
  %tmp2 = fsub double 0.0, 1.0
  store double %tmp2, double* %j

  %fg = alloca i1
  store i1 0, i1* %fg

  %ffg = alloca i1
  store i1 1, i1* %ffg

  %a = alloca i32
  store i32 5, i32* %a

  %b = alloca i32
  store i32 10, i32* %b

  %c = alloca i32
  %tmp3 = load i32, i32* %a
  %tmp4 = load i32, i32* %b
  %tmp5 = add i32 %tmp3, %tmp4

  store i32 %tmp5, i32* %c

  %f = alloca double
  store double 2.5, double* %f

  %g = alloca double
  store double 3.0, double* %g

  %h = alloca double
  %tmp6 = load double, double* %f
  %tmp7 = load double, double* %g
  %tmp8 = fsub double %tmp6, %tmp7

  store double %tmp8, double* %h

  %ok = alloca i1
  %tmp9 = load i32, i32* %c
  %tmp10 = icmp sgt i32 %tmp9, 5

  store i1 %tmp10, i1* %ok

  store i32 8, i32* %i
  store double %tmp12, double* %f
  %tmp11 = load double, double* %f
  %tmp12 = fmul double %tmp11, 2.0

  call i32 @puts(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))
  %tmp13 = load i32, i32* %c
  ret i32 %tmp13
}

