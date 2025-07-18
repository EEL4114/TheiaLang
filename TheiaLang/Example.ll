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
  %u = alloca i32
  %tmp2 = call i32 @m(i32 8)
  %tmp3 = add i32 1, %tmp2

  %tmp4 = add i32 %tmp3, 9

  store i32 %tmp4, i32* %u
  %entity = alloca %Entity
  %tmp5 = getelementptr %Entity, %Entity* %entity, i32 0, i32 0
  store double 70.0, double* %tmp5
  %tmp6 = getelementptr %Entity, %Entity* %entity, i32 0, i32 1
  store double 1.5, double* %tmp6

  %defaultInt = alloca i32
  %defaultFloat = alloca double
  %defaultBool = alloca i1
  %default_s32 = alloca i32
  %default_f32 = alloca double
  %default_bool = alloca i1
  %i = alloca i32
  %tmp7 = sub i32 0, 1
  store i32 %tmp7, i32* %i
  %j = alloca double
  %tmp8 = fsub double 0.0, 1.0
  store double %tmp8, double* %j
  %fg = alloca i1
  store i1 0, i1* %fg
  %ffg = alloca i1
  store i1 1, i1* %ffg
  %a = alloca i32
  store i32 5, i32* %a
  %b = alloca i32
  store i32 10, i32* %b
  %c = alloca i32
  %tmp9 = load i32, i32* %a
  %tmp10 = load i32, i32* %b
  %tmp11 = add i32 %tmp9, %tmp10

  store i32 %tmp11, i32* %c
  %f = alloca double
  store double 2.5, double* %f
  store double %tmp13, double* %f
  %tmp12 = load double, double* %f
  %tmp13 = fmul double %tmp12, 2.0

  %g = alloca double
  store double 3.0, double* %g
  %h = alloca double
  %tmp14 = load double, double* %f
  %tmp15 = load double, double* %g
  %tmp16 = fsub double %tmp14, %tmp15

  store double %tmp16, double* %h
  %ok = alloca i1
  %tmp17 = load i32, i32* %c
  %tmp18 = icmp sgt i32 %tmp17, 5

  store i1 %tmp18, i1* %ok
  call i32 @puts(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))
  %tmp19 = load i32, i32* %c
  ret i32 %tmp19
}

define i32 @m(i32 %j) {
entry:
  %tmp20 = alloca i32
  store i32 %j, i32* %tmp20
  %i = alloca i32
  store i32 3, i32* %i
  store i32 4, i32* %tmp20
  %health = alloca i32
  store i32 7, i32* %health
  call i32 @puts(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))
  ret i32 0
}

