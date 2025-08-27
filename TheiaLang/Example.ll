; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"

declare noalias ptr @malloc(i64) nounwind willreturn
declare void @free(ptr) nounwind

%theia.header = type { i64, i64 }           ; { sizeBytes, magic }
@theia.magic  = internal constant i64 4114, align 8

define internal noalias ptr @__th_allocB(i64 %n) nounwind allocsize(0) {
entry:
  ; total = (n == 0 ? 16 : n + 16)
  %is0   = icmp eq i64 %n, 0
  %np16  = add i64 %n, 16
  %total = select i1 %is0, i64 16, i64 %np16

  %raw   = call noalias ptr @malloc(i64 %total)

  ; write header
  %h_size  = getelementptr %theia.header, ptr %raw, i32 0, i32 0
  store i64 %n, ptr %h_size
  %h_magic = getelementptr %theia.header, ptr %raw, i32 0, i32 1
  %mval    = load i64, ptr @theia.magic
  store i64 %mval, ptr %h_magic

  ; return user pointer = raw + 16
  %user = getelementptr i8, ptr %raw, i64 16
  ret ptr %user
}

define internal void @theia.__th_free(ptr %user) nounwind {
entry:
  ; grab beginning of the allocation header
  %raw     = getelementptr i8, ptr %user, i64 -16
  %h_magic = getelementptr %theia.header, ptr %raw, i32 0, i32 1
  store i64 0, ptr %h_magic
  call void @free(ptr %raw)
  ret void
}

define internal i64 @theia.__th_alloc_size(ptr %user) nounwind {
entry:
  ; grab beginning of the allocation header
  %raw  = getelementptr i8, ptr %user, i64 -16
  %h_sz = getelementptr %theia.header, ptr %raw, i32 0, i32 0
  %n    = load i64, ptr %h_sz
  ret i64 %n
}

; =============================================================================

; ModuleID = 'theia_module'
@.theia_print_str = private constant[19 x i8] c"Hello from Theia!\0A\00"
declare i32 @printf(ptr, ...)
@.print_ret_fmt = private constant [16 x i8] c"%s returned %d\0A\00"

%Vector2 = type { float, float }
%Entity = type { %Transform, float, float, i1 }
%Transform = type { %Vector3, %Vector3, %Vector3 }
%Vector3 = type { float, float, float }
%Dynamic_Array_s8 = type { i64, ptr, i64 }

@.fn_main_str = private constant [5 x i8] c"main\00"
define i32 @main() {
entry:
  %Vec4_main = alloca [4 x float]
  %tmp0 = getelementptr inbounds [4 x float], ptr %Vec4_main, i32 0, i32 1
  store float 3.0, ptr %tmp0
  %ff_main = alloca %Dynamic_Array_s8
  %arrayOfArrays_main = alloca [4 x [6 x i8]]
  %arrX_main = alloca [4 x [6 x ptr]]
  %arrY_main = alloca [4 x ptr]
  %ptrToArrayOfArrays_main = alloca ptr
  %ptrToFloatArray_main = alloca ptr
  %arrOfFloatPtrs_main = alloca [4 x ptr]
  %v_main = alloca ptr
  %tmp1 = call ptr @allocB(i64 4)
  store ptr %tmp1, ptr %v_main
  %voidPtr_main = alloca ptr
  %dynamicArray_main = alloca %Dynamic_Array_s8
  %dynamicArray2_main = alloca %Dynamic_Array_s8
  %z_main = alloca float
  %tmp2 = getelementptr inbounds [4 x float], ptr %Vec4_main, i32 0, i32 2
  %tmp3 = load float, ptr %tmp2
  store float %tmp3, ptr %z_main
  %entity_main = alloca %Entity
  %tmp4 = alloca %Transform
  %tmp5 = alloca %Vector3
  %tmp6 = getelementptr %Vector3, ptr %tmp5, i32 0, i32 0
  store float 0.0, ptr %tmp6
  %tmp7 = getelementptr %Vector3, ptr %tmp5, i32 0, i32 1
  store float 0.0, ptr %tmp7
  %tmp8 = getelementptr %Vector3, ptr %tmp5, i32 0, i32 2
  store float 0.0, ptr %tmp8
  %tmp9 = load %Vector3, ptr %tmp5
  %tmp10 = getelementptr %Transform, ptr %tmp4, i32 0, i32 0
  store %Vector3 %tmp9, ptr %tmp10
  %tmp11 = alloca %Vector3
  %tmp12 = getelementptr %Vector3, ptr %tmp11, i32 0, i32 0
  store float 0.0, ptr %tmp12
  %tmp13 = getelementptr %Vector3, ptr %tmp11, i32 0, i32 1
  store float 0.0, ptr %tmp13
  %tmp14 = getelementptr %Vector3, ptr %tmp11, i32 0, i32 2
  store float 0.0, ptr %tmp14
  %tmp15 = load %Vector3, ptr %tmp11
  %tmp16 = getelementptr %Transform, ptr %tmp4, i32 0, i32 1
  store %Vector3 %tmp15, ptr %tmp16
  %tmp17 = alloca %Vector3
  %tmp18 = getelementptr %Vector3, ptr %tmp17, i32 0, i32 0
  store float 0.0, ptr %tmp18
  %tmp19 = getelementptr %Vector3, ptr %tmp17, i32 0, i32 1
  store float 0.0, ptr %tmp19
  %tmp20 = getelementptr %Vector3, ptr %tmp17, i32 0, i32 2
  store float 0.0, ptr %tmp20
  %tmp21 = load %Vector3, ptr %tmp17
  %tmp22 = getelementptr %Transform, ptr %tmp4, i32 0, i32 2
  store %Vector3 %tmp21, ptr %tmp22
  %tmp23 = load %Transform, ptr %tmp4
  %tmp24 = getelementptr %Entity, ptr %entity_main, i32 0, i32 0
  store %Transform %tmp23, ptr %tmp24
  %tmp25 = getelementptr %Entity, ptr %entity_main, i32 0, i32 1
  store float 70.0, ptr %tmp25
  %tmp26 = getelementptr %Entity, ptr %entity_main, i32 0, i32 2
  store float 1.5, ptr %tmp26
  %tmp27 = getelementptr %Entity, ptr %entity_main, i32 0, i32 3
  store i1 0, ptr %tmp27

  %HP_main = alloca float
  %tmp28 = getelementptr inbounds %Entity, ptr %entity_main, i32 0, i32 1
  %tmp29 = load float, ptr %tmp28
  store float %tmp29, ptr %HP_main
  %healthPtr_main = alloca ptr
  store ptr %HP_main, ptr %healthPtr_main
  %tmp30 = load ptr, ptr %healthPtr_main
  store float 5.0, ptr %tmp30
  %moreHealth_main = alloca float
  %tmp31 = getelementptr inbounds %Entity, ptr %entity_main, i32 0, i32 1
  %tmp32 = load float, ptr %tmp31
  %tmp33 = fadd float 1.0, %tmp32

  store float %tmp33, ptr %moreHealth_main
  store ptr %moreHealth_main, ptr %healthPtr_main
  %tmp34 = load ptr, ptr %healthPtr_main
  store ptr %tmp34, ptr %voidPtr_main
  %tmp35 = load ptr, ptr %healthPtr_main
  %tmp36 = load float, ptr %tmp35
  store float %tmp36, ptr %HP_main
  %tmp37 = load float, ptr %moreHealth_main
  %tmp38 = fadd float %tmp37, 1.0

  store float %tmp38, ptr %moreHealth_main
  %tmp39 = call i32 @m(i32 7)
  %tmp40 = call i1 @n(i1 1)
  %tmp41 = call i1 @n(i1 0)
  %t_main = alloca i32
  %tmp42 = call i32 @m(i32 8)
  store i32 %tmp42, ptr %t_main
  %u_main = alloca i32
  %tmp43 = call i32 @m(i32 8)
  %tmp44 = add i32 1, %tmp43

  %tmp45 = add i32 %tmp44, 9

  store i32 %tmp45, ptr %u_main
  %defaultInt_main = alloca i32
  %defaultFloat_main = alloca float
  %defaultBool_main = alloca i1
  %tmp46 = getelementptr inbounds %Entity, ptr %entity_main, i32 0, i32 1
  store float 4.0, ptr %tmp46
  %default_s32_main = alloca i32
  %default_f32_main = alloca float
  %default_bool_main = alloca i1
  %quad_main = alloca fp128
  store fp128 0xL1C3, ptr %quad_main
  %i_main = alloca i32
  store i32 -1, ptr %i_main
  %tmp47 = load i32, ptr %i_main
  %tmp48 = call i32 @Abs(i32 %tmp47)
  %tmp49 = load i32, ptr %i_main
  %tmp50 = sub i32 0, %tmp49
  %tmp51 = call i32 @Abs(i32 %tmp50)
  %j_main = alloca float
  store float -1.0, ptr %j_main
  %fg_main = alloca i1
  store i1 0, ptr %fg_main
  %ffg_main = alloca i1
  store i1 1, ptr %ffg_main
  %a_main = alloca i32
  store i32 5, ptr %a_main
  %b_main = alloca i32
  store i32 10, ptr %b_main
  %c_main = alloca i32
  %tmp52 = load i32, ptr %a_main
  %tmp53 = load i32, ptr %b_main
  %tmp54 = add i32 %tmp52, %tmp53

  store i32 %tmp54, ptr %c_main
  %d_main = alloca i32
  store i32 4, ptr %d_main
  %tmp55 = load i32, ptr %a_main
  %tmp56 = load i32, ptr %c_main
  %tmp57 = add i32 %tmp55, %tmp56

  store i32 %tmp57, ptr %d_main
  %tmp58 = load i32, ptr %d_main
  %tmp59 = add i32 %tmp58, 42

  store i32 %tmp59, ptr %d_main
  %f_main = alloca float
  store float 2.5, ptr %f_main
  %tmp60 = load float, ptr %f_main
  %tmp61 = fmul float %tmp60, 2.0

  store float %tmp61, ptr %f_main
  %g_main = alloca float
  store float 3.0, ptr %g_main
  %h_main = alloca float
  %tmp62 = load float, ptr %f_main
  %tmp63 = load float, ptr %g_main
  %tmp64 = fsub float %tmp62, %tmp63

  store float %tmp64, ptr %h_main
  %ok_main = alloca i1
  %tmp65 = load i32, ptr %c_main
  %tmp66 = icmp sgt i32 %tmp65, 5

  store i1 %tmp66, ptr %ok_main
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_main_str, i32 0, i32 0), i32 0)
  ret i32 0
}

@.fn_fn_str = private constant [3 x i8] c"fn\00"
define void @fn(i32 %i) {
entry:
  %tmp67 = alloca i32
  store i32 %i, ptr %tmp67
  %j_fn = alloca i32
  %tmp68 = load i32, ptr %tmp67
  %tmp69 = sdiv i32 %tmp68, 7

  store i32 %tmp69, ptr %j_fn
  ret void 
}

@.fn_Branchy_str = private constant [8 x i8] c"Branchy\00"
define i32 @Branchy(i32 %value) {
entry:
  %tmp70 = alloca i32
  store i32 %value, ptr %tmp70
  %tmp71 = load i32, ptr %tmp70
  %tmp72 = icmp eq i32 %tmp71, 0

  br i1 %tmp72, label %if_then_0, label %if_else_0
if_then_0:
  store i32 42, ptr %tmp70
  %f_if_then413 = alloca float
  store float 0.0, ptr %f_if_then413
  store float -1.0, ptr %f_if_then413
  br label %if_end_0
if_else_0:
  store i32 -42, ptr %tmp70
  %f_if_else429 = alloca float
  store float 78.0, ptr %f_if_else429
  br label %if_end_0
if_end_0:
  %tmp73 = load i32, ptr %tmp70
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_Branchy_str, i32 0, i32 0), i32 %tmp73)
  ret i32 %tmp73
}

@.fn_Abs_str = private constant [4 x i8] c"Abs\00"
define i32 @Abs(i32 %value) {
entry:
  %tmp74 = alloca i32
  store i32 %value, ptr %tmp74
  %tmp75 = load i32, ptr %tmp74
  %tmp76 = icmp slt i32 %tmp75, 0

  br i1 %tmp76, label %if_then_1, label %if_end_1
if_then_1:
  %tmp77 = load i32, ptr %tmp74
  %tmp78 = sub i32 0, %tmp77
  ret i32 %tmp78
  br label %if_end_1
if_end_1:
  %tmp79 = load i32, ptr %tmp74
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_Abs_str, i32 0, i32 0), i32 %tmp79)
  ret i32 %tmp79
}

@.fn_Loopy_str = private constant [6 x i8] c"Loopy\00"
define i32 @Loopy(i32 %value) {
entry:
  %tmp80 = alloca i32
  store i32 %value, ptr %tmp80
  %i_Loopy = alloca i32
  store i32 0, ptr %i_Loopy
  br label %for_cond2
for_cond2:
  %tmp81 = load i32, ptr %i_Loopy
  %tmp82 = load i32, ptr %tmp80
  %tmp83 = icmp slt i32 %tmp81, %tmp82

  br i1 %tmp83, label %for_body2, label %for_end2
for_body2:
  %tmp84 = load i32, ptr %tmp80
  %tmp85 = load i32, ptr %i_Loopy
  %tmp86 = add i32 %tmp84, %tmp85

  store i32 %tmp86, ptr %tmp80
  br label %for_iter2
for_iter2:
  %tmp87 = load i32, ptr %i_Loopy
  %tmp88 = sub i32 %tmp87, 3

  store i32 %tmp88, ptr %i_Loopy
  br label %for_cond2
for_end2:
  %tmp89 = load i32, ptr %tmp80
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_Loopy_str, i32 0, i32 0), i32 %tmp89)
  ret i32 %tmp89
}

@.fn_n_str = private constant [2 x i8] c"n\00"
define i1 @n(i1 %b) {
entry:
  %tmp90 = alloca i1
  store i1 %b, ptr %tmp90
  %tmp91 = load i1, ptr %tmp90
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([3 x i8], ptr @.fn_n_str, i32 0, i32 0), i1 %tmp91)
  ret i1 %tmp91
}

@.fn_m_str = private constant [2 x i8] c"m\00"
define i32 @m(i32 %j) {
entry:
  %tmp92 = alloca i32
  store i32 %j, ptr %tmp92
  %i_m = alloca i32
  store i32 3, ptr %i_m
  %tmp93 = load i32, ptr %tmp92
  %tmp94 = add i32 %tmp93, 4

  store i32 %tmp94, ptr %tmp92
  %health_m = alloca i32
  store i32 7, ptr %health_m
  %tmp95 = load i32, ptr %tmp92
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_m_str, i32 0, i32 0), i32 %tmp95)
  ret i32 %tmp95
}

@.fn_IsAlive_str = private constant [8 x i8] c"IsAlive\00"
define i1 @Entity.IsAlive(ptr %this) {
entry:
  %tmp96 = getelementptr %Entity, ptr %this, i32 0, i32 1
  %tmp97 = load float, ptr %tmp96
  %tmp98 = fcmp ogt float %tmp97, 0.0

  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([3 x i8], ptr @.fn_IsAlive_str, i32 0, i32 0), i1 %tmp98)
  ret i1 %tmp98
}

define ptr @allocB(i64 %size) {
entry:
  %tmp99 = alloca i64
  store i64 %size, ptr %tmp99
  %tmp100 = load i64, ptr %tmp99
  %tmp101 = call ptr @__th_allocB(i64 %tmp100)
  ret ptr %tmp101
}

