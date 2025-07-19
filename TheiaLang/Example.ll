; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"
declare i32 @puts(i8*, ...)
@.theia_print_str = private constant[19 x i8] c"Hello from Theia!\0A\00"

%Vector3 = type { float, float, float }
%Entity = type { float, float }

define i1 @Entity.IsAlive(%Entity* %this) {
entry:
  %alive = alloca i1
  %tmp0 = getelementptr %Entity, %Entity* %this, i32 0, i32 0
  %tmp1 = load float, float* %tmp0
  %tmp2 = fcmp ogt float %tmp1, 0.0

  store i1 %tmp2, i1* %alive
  call i32 @puts(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))
  %tmp3 = load i1, i1* %alive
  ret i1 %tmp3
}

define i32 @main() {
entry:
  %t = alloca i32
  %tmp4 = call i32 @m(i32 8)
  store i32 %tmp4, i32* %t
  %u = alloca i32
  %tmp5 = call i32 @m(i32 8)
  %tmp6 = add i32 1, %tmp5

  %tmp7 = add i32 %tmp6, 9

  store i32 %tmp7, i32* %u
  %entity = alloca %Entity
  %tmp8 = getelementptr %Entity, %Entity* %entity, i32 0, i32 0
  store float 70.0, float* %tmp8
  %tmp9 = getelementptr %Entity, %Entity* %entity, i32 0, i32 1
  store float 1.5, float* %tmp9

  %health = alloca float
  %tmp10 = getelementptr inbounds %Entity, %Entity* %entity, i32 0, i32 0
  %tmp11 = load float, float* %tmp10
  store float %tmp11, float* %health
  %moreHealth = alloca float
  %tmp12 = getelementptr inbounds %Entity, %Entity* %entity, i32 0, i32 0
  %tmp13 = load float, float* %tmp12
  %tmp14 = fadd float 1.0, %tmp13

  store float %tmp14, float* %moreHealth
  %defaultInt = alloca i32
  %defaultFloat = alloca float
  %defaultBool = alloca i1
  %default_s32 = alloca i32
  %default_f32 = alloca float
  %default_bool = alloca i1
  %quad = alloca fp128
  %i = alloca i32
  %tmp15 = sub i32 0, 1
  store i32 %tmp15, i32* %i
  %j = alloca float
  %tmp16 = fsub float 0.0, 1.0
  store float %tmp16, float* %j
  %fg = alloca i1
  store i1 0, i1* %fg
  %ffg = alloca i1
  store i1 1, i1* %ffg
  %a = alloca i32
  store i32 5, i32* %a
  %b = alloca i32
  store i32 10, i32* %b
  %c = alloca i32
  %tmp17 = load i32, i32* %a
  %tmp18 = load i32, i32* %b
  %tmp19 = add i32 %tmp17, %tmp18

  store i32 %tmp19, i32* %c
  %d = alloca i32
  store i32 4, i32* %d
  %tmp20 = load i32, i32* %a
  %tmp21 = load i32, i32* %c
  %tmp22 = add i32 %tmp20, %tmp21

  store i32 %tmp22, i32* %d
  %tmp23 = load i32, i32* %d
  %tmp24 = add i32 %tmp23, 42

  store i32 %tmp24, i32* %d
  %f = alloca float
  store float 2.5, float* %f
  %tmp25 = load float, float* %f
  %tmp26 = fmul float %tmp25, 2.0

  store float %tmp26, float* %f
  %g = alloca float
  store float 3.0, float* %g
  %h = alloca float
  %tmp27 = load float, float* %f
  %tmp28 = load float, float* %g
  %tmp29 = fsub float %tmp27, %tmp28

  store float %tmp29, float* %h
  %ok = alloca i1
  %tmp30 = load i32, i32* %c
  %tmp31 = icmp sgt i32 %tmp30, 5

  store i1 %tmp31, i1* %ok
  call i32 @puts(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))
  %tmp32 = load i32, i32* %c
  ret i32 %tmp32
}

define i32 @m(i32 %j) {
entry:
  %tmp33 = alloca i32
  store i32 %j, i32* %tmp33
  %i = alloca i32
  store i32 3, i32* %i
  store i32 4, i32* %tmp33
  %health = alloca i32
  store i32 7, i32* %health
  call i32 @puts(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))
  ret i32 0
}

