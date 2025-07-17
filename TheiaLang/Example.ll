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

define i32 @m(i32 %j) {
entry:
  %tmp1 = alloca i32
  store i32 %j, i32* %tmp1
  %i = alloca i32
  store i32 3, i32* %i

  %health = alloca i32
  store i32 7, i32* %health

  store i32 4, i32* %tmp1
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
  %tmp2 = sub i32 0, 1
  store i32 %tmp2, i32* %i

  %j = alloca double
  %tmp3 = fsub double 0.0, 1.0
  store double %tmp3, double* %j

  %fg = alloca i1
  store i1 0, i1* %fg

  %ffg = alloca i1
  store i1 1, i1* %ffg

  %a = alloca i32
  store i32 5, i32* %a

  %b = alloca i32
  store i32 10, i32* %b

  %c = alloca i32
  %tmp4 = load i32, i32* %a
  %tmp5 = load i32, i32* %b
  %tmp6 = add i32 %tmp4, %tmp5

  store i32 %tmp6, i32* %c

  %f = alloca double
  store double 2.5, double* %f

  %g = alloca double
  store double 3.0, double* %g

  %h = alloca double
  %tmp7 = load double, double* %f
  %tmp8 = load double, double* %g
  %tmp9 = fsub double %tmp7, %tmp8

  store double %tmp9, double* %h

  %ok = alloca i1
  %tmp10 = load i32, i32* %c
  %tmp11 = icmp sgt i32 %tmp10, 5

  store i1 %tmp11, i1* %ok

  %tmp12 = call i32 @m(i32 7)
  store i32 8, i32* %i
  store double %tmp14, double* %f
  %tmp13 = load double, double* %f
  %tmp14 = fmul double %tmp13, 2.0

  call i32 @puts(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))
  %tmp15 = load i32, i32* %c
  ret i32 %tmp15
}

