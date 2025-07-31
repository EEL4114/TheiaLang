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
  %entity_main = alloca %Entity
  %tmp0 = getelementptr %Entity, %Entity* %entity_main, i32 0, i32 0
  store float 70.0, float* %tmp0
  %tmp1 = getelementptr %Entity, %Entity* %entity_main, i32 0, i32 1
  store float 1.5, float* %tmp1

  %HP_main = alloca float
  %tmp2 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 0
  %tmp3 = load float, float* %tmp2
  store float %tmp3, float* %HP_main
  %healthPtr_main = alloca float*
  store float* %HP_main, float** %healthPtr_main
  %tmp4 = load float*, float** %healthPtr_main
  store float 5.0, float* %tmp4
  %moreHealth_main = alloca float
  %tmp5 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 0
  %tmp6 = load float, float* %tmp5
  %tmp7 = fadd float 1.0, %tmp6

  store float %tmp7, float* %moreHealth_main
  store float* %moreHealth_main, float** %healthPtr_main
  %tmp8 = load float, float* %moreHealth_main
  %tmp9 = fadd float %tmp8, 1.0

  store float %tmp9, float* %moreHealth_main
  %tmp10 = call i32 @m(i32 7)
  %tmp11 = call i1 @n(i1 1)
  %tmp12 = call i1 @n(i1 0)
  %t_main = alloca i32
  %tmp13 = call i32 @m(i32 8)
  store i32 %tmp13, i32* %t_main
  %u_main = alloca i32
  %tmp14 = call i32 @m(i32 8)
  %tmp15 = add i32 1, %tmp14

  %tmp16 = add i32 %tmp15, 9

  store i32 %tmp16, i32* %u_main
  %defaultInt_main = alloca i32
  %defaultFloat_main = alloca float
  %defaultBool_main = alloca i1
  %tmp17 = getelementptr inbounds %Entity, %Entity* %entity_main, i32 0, i32 0
  store float 4.0, float* %tmp17
  %default_s32_main = alloca i32
  %default_f32_main = alloca float
  %default_bool_main = alloca i1
  %quad_main = alloca fp128
  store fp128 0xL1C3, fp128* %quad_main
  %i_main = alloca i32
  %tmp18 = sub i32 0, 1
  store i32 %tmp18, i32* %i_main
  %tmp19 = load i32, i32* %i_main
  %tmp20 = call i32 @Abs(i32 %tmp19)
  %tmp21 = load i32, i32* %i_main
  %tmp22 = sub i32 0, %tmp21
  %tmp23 = call i32 @Abs(i32 %tmp22)
  %j_main = alloca float
  %tmp24 = fsub float 0.0, 1.0
  store float %tmp24, float* %j_main
  %fg_main = alloca i1
  store i1 0, i1* %fg_main
  %ffg_main = alloca i1
  store i1 1, i1* %ffg_main
  %a_main = alloca i32
  store i32 5, i32* %a_main
  %b_main = alloca i32
  store i32 10, i32* %b_main
  %c_main = alloca i32
  %tmp25 = load i32, i32* %a_main
  %tmp26 = load i32, i32* %b_main
  %tmp27 = add i32 %tmp25, %tmp26

  store i32 %tmp27, i32* %c_main
  %d_main = alloca i32
  store i32 4, i32* %d_main
  %tmp28 = load i32, i32* %a_main
  %tmp29 = load i32, i32* %c_main
  %tmp30 = add i32 %tmp28, %tmp29

  store i32 %tmp30, i32* %d_main
  %tmp31 = load i32, i32* %d_main
  %tmp32 = add i32 %tmp31, 42

  store i32 %tmp32, i32* %d_main
  %f_main = alloca float
  store float 2.5, float* %f_main
  %tmp33 = load float, float* %f_main
  %tmp34 = fmul float %tmp33, 2.0

  store float %tmp34, float* %f_main
  %g_main = alloca float
  store float 3.0, float* %g_main
  %h_main = alloca float
  %tmp35 = load float, float* %f_main
  %tmp36 = load float, float* %g_main
  %tmp37 = fsub float %tmp35, %tmp36

  store float %tmp37, float* %h_main
  %ok_main = alloca i1
  %tmp38 = load i32, i32* %c_main
  %tmp39 = icmp sgt i32 %tmp38, 5

  store i1 %tmp39, i1* %ok_main
  %tmp40 = load i32, i32* %c_main
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_main_str, i32 0, i32 0), i32 %tmp40)
  ret i32 %tmp40
}

@.fn_Branchy_str = private constant [8 x i8] c"Branchy\00"
define i32 @Branchy(i32 %value) {
entry:
  %tmp41 = alloca i32
  store i32 %value, i32* %tmp41
  %tmp42 = load i32, i32* %tmp41
  %tmp43 = icmp eq i32 %tmp42, 0

  br i1 %tmp43, label %if_then_0, label %if_else_0
if_then_0:
  store i32 42, i32* %tmp41
  %f_if_then252 = alloca float
  store float 0.0, float* %f_if_then252
  %tmp44 = fsub float 0.0, 1.0
  store float %tmp44, float* %f_if_then252
  br label %if_end_0
if_else_0:
  %tmp45 = sub i32 0, 42
  store i32 %tmp45, i32* %tmp41
  %f_if_else268 = alloca float
  store float 78.0, float* %f_if_else268
  br label %if_end_0
if_end_0:
  %tmp46 = load i32, i32* %tmp41
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Branchy_str, i32 0, i32 0), i32 %tmp46)
  ret i32 %tmp46
}

@.fn_Abs_str = private constant [4 x i8] c"Abs\00"
define i32 @Abs(i32 %value) {
entry:
  %tmp47 = alloca i32
  store i32 %value, i32* %tmp47
  %tmp48 = load i32, i32* %tmp47
  %tmp49 = icmp slt i32 %tmp48, 0

  br i1 %tmp49, label %if_then_1, label %if_end_1
if_then_1:
  %tmp50 = load i32, i32* %tmp47
  %tmp51 = sub i32 0, %tmp50
  ret i32 %tmp51
  br label %if_end_1
if_end_1:
  %tmp52 = load i32, i32* %tmp47
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Abs_str, i32 0, i32 0), i32 %tmp52)
  ret i32 %tmp52
}

@.fn_Loopy_str = private constant [6 x i8] c"Loopy\00"
define i32 @Loopy(i32 %value) {
entry:
  %tmp53 = alloca i32
  store i32 %value, i32* %tmp53
  %i_Loopy = alloca i32
  store i32 0, i32* %i_Loopy
  br label %for_cond2
for_cond2:
  %tmp54 = load i32, i32* %i_Loopy
  %tmp55 = load i32, i32* %tmp53
  %tmp56 = icmp slt i32 %tmp54, %tmp55

  br i1 %tmp56, label %for_body2, label %for_end2
for_body2:
  %tmp57 = load i32, i32* %tmp53
  %tmp58 = load i32, i32* %i_Loopy
  %tmp59 = add i32 %tmp57, %tmp58

  store i32 %tmp59, i32* %tmp53
  br label %for_iter2
for_iter2:
  %tmp60 = load i32, i32* %i_Loopy
  %tmp61 = sub i32 %tmp60, 3

  store i32 %tmp61, i32* %i_Loopy
  br label %for_cond2
for_end2:
  %tmp62 = load i32, i32* %tmp53
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_Loopy_str, i32 0, i32 0), i32 %tmp62)
  ret i32 %tmp62
}

@.fn_n_str = private constant [2 x i8] c"n\00"
define i1 @n(i1 %b) {
entry:
  %tmp63 = alloca i1
  store i1 %b, i1* %tmp63
  %tmp64 = load i1, i1* %tmp63
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([3 x i8], [3 x i8]* @.fn_n_str, i32 0, i32 0), i1 %tmp64)
  ret i1 %tmp64
}

@.fn_m_str = private constant [2 x i8] c"m\00"
define i32 @m(i32 %j) {
entry:
  %tmp65 = alloca i32
  store i32 %j, i32* %tmp65
  %i_m = alloca i32
  store i32 3, i32* %i_m
  %tmp66 = load i32, i32* %tmp65
  %tmp67 = add i32 %tmp66, 4

  store i32 %tmp67, i32* %tmp65
  %health_m = alloca i32
  store i32 7, i32* %health_m
  %tmp68 = load i32, i32* %tmp65
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fn_m_str, i32 0, i32 0), i32 %tmp68)
  ret i32 %tmp68
}

@.fn_IsAlive_str = private constant [8 x i8] c"IsAlive\00"
define i1 @Entity.IsAlive(%Entity* %this) {
entry:
  %tmp69 = getelementptr %Entity, %Entity* %this, i32 0, i32 0
  %tmp70 = load float, float* %tmp69
  %tmp71 = fcmp ogt float %tmp70, 0.0

  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), i8* getelementptr inbounds ([3 x i8], [3 x i8]* @.fn_IsAlive_str, i32 0, i32 0), i1 %tmp71)
  ret i1 %tmp71
}

