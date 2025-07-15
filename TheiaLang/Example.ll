; ModuleID = 'theia_module'
declare i32 @puts(i8*, ...)
@.theia_print_str = private constant[19 x i8] c"Hello from Theia!\0A\00"

%Vector3 = type { double, double, double }
%Entity = type { double }

define i32 @Entity.Kill(%Entity* %this) {
entry:
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
  %tmp0 = sub i32 0, 1
  store i32 %tmp0, i32* %i

  %j = alloca double
  %tmp1 = fsub double 0.0, 1.0
  store double %tmp1, double* %j

  %fg = alloca i1
  store i1 0, i1* %fg

  %ffg = alloca i1
  store i1 1, i1* %ffg

  %a = alloca i32
  store i32 5, i32* %a

  %b = alloca i32
  store i32 10, i32* %b

  %c = alloca i32
  %tmp2 = load i32, i32* %a
  %tmp3 = load i32, i32* %b
  %tmp4 = add i32 %tmp2, %tmp3

  store i32 %tmp4, i32* %c

  %f = alloca double
  store double 2.5, double* %f

  %g = alloca double
  store double 3.0, double* %g

  %h = alloca double
  %tmp5 = load double, double* %f
  %tmp6 = load double, double* %g
  %tmp7 = fsub double %tmp5, %tmp6

  store double %tmp7, double* %h

  %ok = alloca i1
  %tmp8 = load i32, i32* %c
  %tmp9 = icmp sgt i32 %tmp8, 5

  store i1 %tmp9, i1* %ok

  store i32 8, i32* %i
  %tmp10 = load double, double* %f
  %tmp11 = fmul double %tmp10, 2.0

  store double %tmp11, double* %f
call i32 @puts(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))
  %tmp12 = load i32, i32* %c
  ret i32 %tmp12
}

