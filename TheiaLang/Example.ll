; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"
declare i32 @puts(i8*, ...)
@.theia_print_str = private constant[19 x i8] c"Hello from Theia!\0A\00"

%Vector3 = type { float, float, float }
%Entity = type { float, float }

define i32 @main() {
entry:
  %tmp0 = call i32 @m(i32 7)
  %tmp1 = call i1 @n()
  %t = alloca i32
  %tmp2 = call i32 @m(i32 8)
  store i32 %tmp2, i32* %t
  %u = alloca i32
  %tmp3 = call i32 @m(i32 8)
  %tmp4 = add i32 1, %tmp3

  %tmp5 = add i32 %tmp4, 9

  store i32 %tmp5, i32* %u
  %entity = alloca %Entity
  %tmp6 = getelementptr %Entity, %Entity* %entity, i32 0, i32 0
  store float 70.0, float* %tmp6
  %tmp7 = getelementptr %Entity, %Entity* %entity, i32 0, i32 1
  store float 1.5, float* %tmp7

  %HP = alloca float
  %tmp8 = getelementptr inbounds %Entity, %Entity* %entity, i32 0, i32 0
  %tmp9 = load float, float* %tmp8
  store float %tmp9, float* %HP
  %moreHealth = alloca float
  %tmp10 = getelementptr inbounds %Entity, %Entity* %entity, i32 0, i32 0
  %tmp11 = load float, float* %tmp10
  %tmp12 = fadd float 1.0, %tmp11

  store float %tmp12, float* %moreHealth
  %defaultInt = alloca i32
  %defaultFloat = alloca float
  %defaultBool = alloca i1
  %tmp13 = getelementptr inbounds %Entity, %Entity* %entity, i32 0, i32 0
  store float 4.0, float* %tmp13
  %default_s32 = alloca i32
  %default_f32 = alloca float
  %default_bool = alloca i1
  %quad = alloca fp128
  store fp128 0xL1C3, fp128* %quad
  %i = alloca i32
  %tmp14 = sub i32 0, 1
  store i32 %tmp14, i32* %i
  %j = alloca float
  %tmp15 = fsub float 0.0, 1.0
  store float %tmp15, float* %j
  %fg = alloca i1
  store i1 0, i1* %fg
  %ffg = alloca i1
  store i1 1, i1* %ffg
  %a = alloca i32
  store i32 5, i32* %a
  %b = alloca i32
  store i32 10, i32* %b
  %c = alloca i32
  %tmp16 = load i32, i32* %a
  %tmp17 = load i32, i32* %b
  %tmp18 = add i32 %tmp16, %tmp17

  store i32 %tmp18, i32* %c
  %d = alloca i32
  store i32 4, i32* %d
  %tmp19 = load i32, i32* %a
  %tmp20 = load i32, i32* %c
  %tmp21 = add i32 %tmp19, %tmp20

  store i32 %tmp21, i32* %d
  %tmp22 = load i32, i32* %d
  %tmp23 = add i32 %tmp22, 42

  store i32 %tmp23, i32* %d
  %f = alloca float
  store float 2.5, float* %f
  %tmp24 = load float, float* %f
  %tmp25 = fmul float %tmp24, 2.0

  store float %tmp25, float* %f
  %g = alloca float
  store float 3.0, float* %g
  %h = alloca float
  %tmp26 = load float, float* %f
  %tmp27 = load float, float* %g
  %tmp28 = fsub float %tmp26, %tmp27

  store float %tmp28, float* %h
  %ok = alloca i1
  %tmp29 = load i32, i32* %c
  %tmp30 = icmp sgt i32 %tmp29, 5

  store i1 %tmp30, i1* %ok
  call i32 @puts(i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))
  %tmp31 = load i32, i32* %c
  ret i32 %tmp31
}

define i32 @m(i32 %j) {
entry:
  %tmp32 = alloca i32
  store i32 %j, i32* %tmp32
  %i = alloca i32
  store i32 3, i32* %i
  store i32 4, i32* %tmp32
  %health = alloca i32
  store i32 7, i32* %health
  ret i32 0
}

define i1 @n() {
entry:
  %tmp33 = and i1 0, 0

  ret i1 %tmp33
}

define i1 @Entity.IsAlive(%Entity* %this) {
entry:
  %tmp34 = getelementptr %Entity, %Entity* %this, i32 0, i32 0
  %tmp35 = load float, float* %tmp34
  %tmp36 = fcmp ogt float %tmp35, 0.0

  ret i1 %tmp36
}

