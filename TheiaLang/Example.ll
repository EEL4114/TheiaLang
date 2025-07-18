; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"
declare i32 @puts(i8*, ...)
@.theia_print_str = private constant[19 x i8] c"Hello from Theia!\0A\00"

%Vector3 = type { double, double, double }
%Entity = type { double, double }

define i32 @Entity.AddHealth(%Entity* %this, double %amount) {
entry:
  %tmp0 = alloca double
  store double %amount, double* %tmp0
  store double 0.0, double* %tmp0
  call i32 @puts(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))
  ret i32 0
}

define i32 @main() {
entry:
  %t = alloca i32
  %tmp1 = call i32 @m(i32 8)
  store i32 %tmp1, i32* %t
  %entity = alloca %Entity
  %tmp2 = getelementptr %Entity, %Entity* %entity, i32 0, i32 0
  store double 70.0, double* %tmp2
  %tmp3 = getelementptr %Entity, %Entity* %entity, i32 0, i32 1
  store double 1.5, double* %tmp3

  %defaultInt = alloca i32
  %defaultFloat = alloca double
  %defaultBool = alloca i1
  %default_s32 = alloca i32
  %default_f32 = alloca double
  %default_bool = alloca i1
  %i = alloca i32
  %tmp4 = sub i32 0, 1
  store i32 %tmp4, i32* %i
  %j = alloca double
  %tmp5 = fsub double 0.0, 1.0
  store double %tmp5, double* %j
  %fg = alloca i1
  store i1 0, i1* %fg
  %ffg = alloca i1
  store i1 1, i1* %ffg
  %a = alloca i32
  store i32 5, i32* %a
  %b = alloca i32
  store i32 10, i32* %b
  %c = alloca i32
  %tmp6 = load i32, i32* %a
  %tmp7 = load i32, i32* %b
  %tmp8 = add i32 %tmp6, %tmp7

  store i32 %tmp8, i32* %c
  %f = alloca double
  store double 2.5, double* %f
  store double %tmp10, double* %f
  %tmp9 = load double, double* %f
  %tmp10 = fmul double %tmp9, 2.0

  %g = alloca double
  store double 3.0, double* %g
  %h = alloca double
  %tmp11 = load double, double* %f
  %tmp12 = load double, double* %g
  %tmp13 = fsub double %tmp11, %tmp12

  store double %tmp13, double* %h
  %ok = alloca i1
  %tmp14 = load i32, i32* %c
  %tmp15 = icmp sgt i32 %tmp14, 5

  store i1 %tmp15, i1* %ok
  call i32 @puts(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))
  %tmp16 = load i32, i32* %c
  ret i32 %tmp16
}

define i32 @m(i32 %j) {
entry:
  %tmp17 = alloca i32
  store i32 %j, i32* %tmp17
  %i = alloca i32
  store i32 3, i32* %i
  store i32 4, i32* %tmp17
  %health = alloca i32
  store i32 7, i32* %health
  call i32 @puts(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))
  ret i32 0
}

