; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"
declare i32 @puts(i8*, ...)
@.theia_print_str = private constant[19 x i8] c"Hello from Theia!\0A\00"

%Vector3 = type { double, double, double }
%Entity = type { double, double }

define i1 @Entity.IsAlive(%Entity* %this) {
entry:
  call i32 @puts(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))
  ret i1 1
}

define i32 @main() {
entry:
  %t = alloca i32
  %tmp0 = call i32 @m(i32 8)
  store i32 %tmp0, i32* %t
  %u = alloca i32
  %tmp1 = call i32 @m(i32 8)
  %tmp2 = add i32 1, %tmp1

  %tmp3 = add i32 %tmp2, 9

  store i32 %tmp3, i32* %u
  %entity = alloca %Entity
  %tmp4 = getelementptr %Entity, %Entity* %entity, i32 0, i32 0
  store double 70.0, double* %tmp4
  %tmp5 = getelementptr %Entity, %Entity* %entity, i32 0, i32 1
  store double 1.5, double* %tmp5

  %health = alloca double
  %tmp6 = getelementptr inbounds %Entity, %Entity* %entity, i32 0, i32 0
  %tmp7 = load double, double* %tmp6
  store double %tmp7, double* %health
  %moreHealth = alloca double
  %tmp8 = getelementptr inbounds %Entity, %Entity* %entity, i32 0, i32 0
  %tmp9 = load double, double* %tmp8
  %tmp10 = fadd double 1.0, %tmp9

  store double %tmp10, double* %moreHealth
  %defaultInt = alloca i32
  %defaultFloat = alloca double
  %defaultBool = alloca i1
  %default_s32 = alloca i32
  %default_f32 = alloca double
  %default_bool = alloca i1
  %i = alloca i32
  %tmp11 = sub i32 0, 1
  store i32 %tmp11, i32* %i
  %j = alloca double
  %tmp12 = fsub double 0.0, 1.0
  store double %tmp12, double* %j
  %fg = alloca i1
  store i1 0, i1* %fg
  %ffg = alloca i1
  store i1 1, i1* %ffg
  %a = alloca i32
  store i32 5, i32* %a
  %b = alloca i32
  store i32 10, i32* %b
  %c = alloca i32
  %tmp13 = load i32, i32* %a
  %tmp14 = load i32, i32* %b
  %tmp15 = add i32 %tmp13, %tmp14

  store i32 %tmp15, i32* %c
  %d = alloca i32
  store i32 4, i32* %d
  store i32 %tmp18, i32* %d
  %tmp16 = load i32, i32* %a
  %tmp17 = load i32, i32* %c
  %tmp18 = add i32 %tmp16, %tmp17

  store i32 %tmp20, i32* %d
  %tmp19 = load i32, i32* %d
  %tmp20 = add i32 %tmp19, 42

  %f = alloca double
  store double 2.5, double* %f
  store double %tmp22, double* %f
  %tmp21 = load double, double* %f
  %tmp22 = fmul double %tmp21, 2.0

  %g = alloca double
  store double 3.0, double* %g
  %h = alloca double
  %tmp23 = load double, double* %f
  %tmp24 = load double, double* %g
  %tmp25 = fsub double %tmp23, %tmp24

  store double %tmp25, double* %h
  %ok = alloca i1
  %tmp26 = load i32, i32* %c
  %tmp27 = icmp sgt i32 %tmp26, 5

  store i1 %tmp27, i1* %ok
  call i32 @puts(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))
  %tmp28 = load i32, i32* %c
  ret i32 %tmp28
}

define i32 @m(i32 %j) {
entry:
  %tmp29 = alloca i32
  store i32 %j, i32* %tmp29
  %i = alloca i32
  store i32 3, i32* %i
  store i32 4, i32* %tmp29
  %health = alloca i32
  store i32 7, i32* %health
  call i32 @puts(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))
  ret i32 0
}

