; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"
declare i32 @puts(i8*, ...)
@.theia_print_str = private constant[19 x i8] c"Hello from Theia!\0A\00"
declare i32 @printf(i8*, ...)
@.print_ret_fmt = private constant [16 x i8] c"%s returned %d\0A\00"

%Vector3 = type { float, float, float }
%Entity = type { float, float }

@.fn_main_str = private constant [5 x i8] c"main\00"
define i32 @main() {
entry:
  %tmp0 = call i32 @m(i32 7)
  %tmp1 = call i1 @n(i1 1)
  %tmp2 = call i1 @n(i1 0)
  %t = alloca i32
  %tmp3 = call i32 @m(i32 8)
  store i32 %tmp3, i32* %t
  %u = alloca i32
  %tmp4 = call i32 @m(i32 8)
  %tmp5 = add i32 1, %tmp4

  %tmp6 = add i32 %tmp5, 9

  store i32 %tmp6, i32* %u
  %entity = alloca %Entity
  %tmp7 = getelementptr %Entity, %Entity* %entity, i32 0, i32 0
  store float 70.0, float* %tmp7
  %tmp8 = getelementptr %Entity, %Entity* %entity, i32 0, i32 1
  store float 1.5, float* %tmp8

  %HP = alloca float
  %tmp9 = getelementptr inbounds %Entity, %Entity* %entity, i32 0, i32 0
  %tmp10 = load float, float* %tmp9
  store float %tmp10, float* %HP
  %moreHealth = alloca float
  %tmp11 = getelementptr inbounds %Entity, %Entity* %entity, i32 0, i32 0
  %tmp12 = load float, float* %tmp11
  %tmp13 = fadd float 1.0, %tmp12

  store float %tmp13, float* %moreHealth
  %defaultInt = alloca i32
  %defaultFloat = alloca float
  %defaultBool = alloca i1
  %tmp14 = getelementptr inbounds %Entity, %Entity* %entity, i32 0, i32 0
  store float 4.0, float* %tmp14
  %default_s32 = alloca i32
  %default_f32 = alloca float
  %default_bool = alloca i1
  %quad = alloca fp128
  store fp128 0xL1C3, fp128* %quad
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
  %tmp32 = load i32, i32* %c
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_main_str, i32 0, i32 0), i32 %tmp32)
  ret i32 %tmp32
}

@.fn_n_str = private constant [2 x i8] c"n\00"
define i1 @n(i1 %b) {
entry:
  %tmp33 = alloca i1
  store i1 %b, i1* %tmp33
  %tmp34 = load i1, i1* %tmp33
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([3 x i8], [3 x i8]* @.fn_n_str, i32 0, i32 0), i1 %tmp34)
  ret i1 %tmp34
}

@.fn_m_str = private constant [2 x i8] c"m\00"
define i32 @m(i32 %j) {
entry:
  %tmp35 = alloca i32
  store i32 %j, i32* %tmp35
  %i = alloca i32
  store i32 3, i32* %i
  %tmp36 = load i32, i32* %tmp35
  %tmp37 = add i32 %tmp36, 4

  store i32 %tmp37, i32* %tmp35
  %health = alloca i32
  store i32 7, i32* %health
  %tmp38 = load i32, i32* %tmp35
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_m_str, i32 0, i32 0), i32 %tmp38)
  ret i32 %tmp38
}

@.fn_IsAlive_str = private constant [8 x i8] c"IsAlive\00"
define i1 @Entity.IsAlive(%Entity* %this) {
entry:
  %tmp39 = getelementptr %Entity, %Entity* %this, i32 0, i32 0
  %tmp40 = load float, float* %tmp39
  %tmp41 = fcmp ogt float %tmp40, 0.0

  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([3 x i8], [3 x i8]* @.fn_IsAlive_str, i32 0, i32 0), i1 %tmp41)
  ret i1 %tmp41
}

