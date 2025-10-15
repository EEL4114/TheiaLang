; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"

declare noalias ptr @realloc(i64)
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

define internal noalias ptr @__th_reallocB(ptr %user, i64 %newB) nounwind {
entry:
  ; user ptr == null → behaves like alloc
  %isNull = icmp eq ptr %user, null
  br i1 %isNull, label %alloc, label %have

alloc:
  %retA = call noalias ptr @__th_allocB(i64 %newB)
  ret ptr %retA

have:
  ; newBytes == 0 → free and return null (so arrays can have data = null, cap = 0)
  %isZero = icmp eq i64 %newB, 0
  br i1 %isZero, label %freeNull, label %grow

freeNull:
  call void @__th_free(ptr %user)
  ret ptr null

grow:
  %raw    = getelementptr i8, ptr %user, i64 -16
  %total  = add i64 %newB, 16
  %newRaw = call ptr @realloc(ptr %raw, i64 %total)
  ; update header.size
  %h_sz = getelementptr %theia.header, ptr %newRaw, i32 0, i32 0
  store i64 %newB, ptr %h_sz
  ; return user pointer
  %user2 = getelementptr i8, ptr %newRaw, i64 16
  ret ptr %user2
}

define internal void @__th_free(ptr %user) nounwind {
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
  %fx_main = alloca float
  store float 2.5, ptr %fx_main
  %sw_main = alloca i16
  %tmp0 = load float, ptr %fx_main
  %tmp1 = fptosi float %tmp0 to i16
  store i16 %tmp1, ptr %sw_main
  %sx_main = alloca i32
  %tmp2 = load i16, ptr %sw_main
  %tmp3 = sext i16 %tmp2 to i32
  store i32 %tmp3, ptr %sx_main
  %tmp4 = load i32, ptr %sx_main
  store i32 %tmp4, ptr %sx_main
  %tmp5 = load i32, ptr %sx_main
  %tmp6 = trunc i32 %tmp5 to i8
  %tmp7 = sext i8 %tmp6 to i32
  store i32 %tmp7, ptr %sx_main
  %sa_main = alloca i8
  %tmp8 = load i32, ptr %sx_main
  %tmp9 = trunc i32 %tmp8 to i8
  store i8 %tmp9, ptr %sa_main
  %Vec4_main = alloca [4 x float]
  %tmp10 = getelementptr inbounds [4 x float], ptr %Vec4_main, i32 0, i32 1
  store float 3.0, ptr %tmp10
  %ff_main = alloca %Dynamic_Array_s8
  %arrayOfArrays_main = alloca [4 x [6 x i8]]
  %arrX_main = alloca [4 x [6 x ptr]]
  %arrY_main = alloca [4 x ptr]
  %ptrToArrayOfArrays_main = alloca ptr
  %ptrToFloatArray_main = alloca ptr
  %arrOfFloatPtrs_main = alloca [4 x ptr]
  %v_main = alloca ptr
  %tmp11 = call ptr @AllocB(i64 4)
  store ptr %tmp11, ptr %v_main
  %tmp12 = load ptr, ptr %v_main
  %tmp13 = call ptr @ReallocB(ptr %tmp12, i64 8)
  store ptr %tmp13, ptr %v_main
  %voidPtr_main = alloca ptr
  %dynamicArray_main = alloca %Dynamic_Array_s8
  %dynamicArray2_main = alloca %Dynamic_Array_s8
  %z_main = alloca float
  %tmp14 = getelementptr inbounds [4 x float], ptr %Vec4_main, i32 0, i32 2
  %tmp15 = load float, ptr %tmp14
  store float %tmp15, ptr %z_main
  %entity_main = alloca %Entity
  %tmp16 = alloca %Transform
  %tmp17 = alloca %Vector3
  %tmp18 = getelementptr %Vector3, ptr %tmp17, i32 0, i32 0
  store float 1.0, ptr %tmp18
  %tmp19 = getelementptr %Vector3, ptr %tmp17, i32 0, i32 1
  store float 1.0, ptr %tmp19
  %tmp20 = getelementptr %Vector3, ptr %tmp17, i32 0, i32 2
  store float 1.0, ptr %tmp20
  %tmp21 = load %Vector3, ptr %tmp17
  %tmp22 = getelementptr %Transform, ptr %tmp16, i32 0, i32 0
  store %Vector3 %tmp21, ptr %tmp22
  %tmp23 = alloca %Vector3
  %tmp24 = getelementptr %Vector3, ptr %tmp23, i32 0, i32 0
  store float 1.0, ptr %tmp24
  %tmp25 = getelementptr %Vector3, ptr %tmp23, i32 0, i32 1
  store float 1.0, ptr %tmp25
  %tmp26 = getelementptr %Vector3, ptr %tmp23, i32 0, i32 2
  store float 1.0, ptr %tmp26
  %tmp27 = load %Vector3, ptr %tmp23
  %tmp28 = getelementptr %Transform, ptr %tmp16, i32 0, i32 1
  store %Vector3 %tmp27, ptr %tmp28
  %tmp29 = alloca %Vector3
  %tmp30 = getelementptr %Vector3, ptr %tmp29, i32 0, i32 0
  store float 1.0, ptr %tmp30
  %tmp31 = getelementptr %Vector3, ptr %tmp29, i32 0, i32 1
  store float 1.0, ptr %tmp31
  %tmp32 = getelementptr %Vector3, ptr %tmp29, i32 0, i32 2
  store float 1.0, ptr %tmp32
  %tmp33 = load %Vector3, ptr %tmp29
  %tmp34 = getelementptr %Transform, ptr %tmp16, i32 0, i32 2
  store %Vector3 %tmp33, ptr %tmp34
  %tmp35 = load %Transform, ptr %tmp16
  %tmp36 = getelementptr %Entity, ptr %entity_main, i32 0, i32 0
  store %Transform %tmp35, ptr %tmp36
  %tmp37 = getelementptr %Entity, ptr %entity_main, i32 0, i32 1
  store float 70.0, ptr %tmp37
  %tmp38 = getelementptr %Entity, ptr %entity_main, i32 0, i32 2
  store float 1.5, ptr %tmp38
  %tmp39 = getelementptr %Entity, ptr %entity_main, i32 0, i32 3
  store i1 0, ptr %tmp39

  %ii_main = alloca i32
  store i32 45, ptr %ii_main
  %jj_main = alloca i32
  store i32 4, ptr %jj_main
  %HP_main = alloca float
  %tmp40 = getelementptr inbounds %Entity, ptr %entity_main, i32 0, i32 1
  %tmp41 = load float, ptr %tmp40
  store float %tmp41, ptr %HP_main
  %healthPtr_main = alloca ptr
  store ptr %HP_main, ptr %healthPtr_main
  %tmp42 = load ptr, ptr %healthPtr_main
  store float 5.0, ptr %tmp42
  %moreHealth_main = alloca float
  %tmp43 = getelementptr inbounds %Entity, ptr %entity_main, i32 0, i32 1
  %tmp44 = load float, ptr %tmp43
  %tmp45 = fadd float 1.0, %tmp44

  store float %tmp45, ptr %moreHealth_main
  store ptr %moreHealth_main, ptr %healthPtr_main
  %tmp46 = load ptr, ptr %healthPtr_main
  store ptr %tmp46, ptr %voidPtr_main
  %tmp47 = load ptr, ptr %healthPtr_main
  %tmp48 = load float, ptr %tmp47
  store float %tmp48, ptr %HP_main
  %tmp49 = load float, ptr %moreHealth_main
  %tmp50 = fadd float %tmp49, 1.0

  store float %tmp50, ptr %moreHealth_main
  %tmp51 = call i32 @m(i32 7)
  %tmp52 = call i1 @n(i1 1)
  %tmp53 = call i1 @n(i1 0)
  %t_main = alloca i32
  %tmp54 = call i32 @m(i32 8)
  store i32 %tmp54, ptr %t_main
  %u_main = alloca i32
  %tmp55 = call i32 @m(i32 8)
  %tmp56 = add i32 1, %tmp55

  %tmp57 = add i32 %tmp56, 9

  store i32 %tmp57, ptr %u_main
  %defaultInt_main = alloca i32
  %defaultFloat_main = alloca float
  %defaultBool_main = alloca i1
  %tmp58 = getelementptr inbounds %Entity, ptr %entity_main, i32 0, i32 1
  store float 4.0, ptr %tmp58
  %default_s32_main = alloca i32
  %default_f32_main = alloca float
  %default_bool_main = alloca i1
  %quad_main = alloca fp128
  store fp128 0xL1C3, ptr %quad_main
  %i_main = alloca i32
  store i32 -1, ptr %i_main
  %tmp59 = load i32, ptr %i_main
  %tmp60 = call i32 @Abs(i32 %tmp59)
  %tmp61 = load i32, ptr %i_main
  %tmp62 = sub i32 0, %tmp61
  %tmp63 = call i32 @Abs(i32 %tmp62)
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
  %tmp64 = load i32, ptr %a_main
  %tmp65 = load i32, ptr %b_main
  %tmp66 = add i32 %tmp64, %tmp65

  store i32 %tmp66, ptr %c_main
  %d_main = alloca i32
  store i32 4, ptr %d_main
  %tmp67 = load i32, ptr %a_main
  %tmp68 = load i32, ptr %c_main
  %tmp69 = add i32 %tmp67, %tmp68

  store i32 %tmp69, ptr %d_main
  %tmp70 = load i32, ptr %d_main
  %tmp71 = add i32 %tmp70, 42

  store i32 %tmp71, ptr %d_main
  %f_main = alloca float
  store float 2.5, ptr %f_main
  %tmp72 = load float, ptr %f_main
  %tmp73 = fmul float %tmp72, 2.0

  store float %tmp73, ptr %f_main
  %g_main = alloca float
  store float 3.0, ptr %g_main
  %h_main = alloca float
  %tmp74 = load float, ptr %f_main
  %tmp75 = load float, ptr %g_main
  %tmp76 = fsub float %tmp74, %tmp75

  store float %tmp76, ptr %h_main
  %ok_main = alloca i1
  %tmp77 = load i32, ptr %c_main
  %tmp78 = icmp sgt i32 %tmp77, 5

  store i1 %tmp78, ptr %ok_main
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_main_str, i32 0, i32 0), i32 0)
  ret i32 0
}

@.fn_fn_str = private constant [3 x i8] c"fn\00"
define void @fn(i32 %i) {
entry:
  %tmp79 = alloca i32
  store i32 %i, ptr %tmp79
  %j_fn = alloca i32
  %tmp80 = load i32, ptr %tmp79
  %tmp81 = sdiv i32 %tmp80, 7

  store i32 %tmp81, ptr %j_fn
  ret void 
}

@.fn_Branchy_str = private constant [8 x i8] c"Branchy\00"
define i32 @Branchy(i32 %value) {
entry:
  %tmp82 = alloca i32
  store i32 %value, ptr %tmp82
  %tmp83 = load i32, ptr %tmp82
  %tmp84 = icmp eq i32 %tmp83, 0

  br i1 %tmp84, label %if_then_0, label %if_else_0
if_then_0:
  store i32 42, ptr %tmp82
  %f_if_then483 = alloca float
  store float 0.0, ptr %f_if_then483
  store float -1.0, ptr %f_if_then483
  br label %if_end_0
if_else_0:
  store i32 -42, ptr %tmp82
  %f_if_else499 = alloca float
  store float 78.0, ptr %f_if_else499
  br label %if_end_0
if_end_0:
  %tmp85 = load i32, ptr %tmp82
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_Branchy_str, i32 0, i32 0), i32 %tmp85)
  ret i32 %tmp85
}

@.fn_Abs_str = private constant [4 x i8] c"Abs\00"
define i32 @Abs(i32 %value) {
entry:
  %tmp86 = alloca i32
  store i32 %value, ptr %tmp86
  %tmp87 = load i32, ptr %tmp86
  %tmp88 = icmp slt i32 %tmp87, 0

  br i1 %tmp88, label %if_then_1, label %if_end_1
if_then_1:
  %tmp89 = load i32, ptr %tmp86
  %tmp90 = sub i32 0, %tmp89
  ret i32 %tmp90
  br label %if_end_1
if_end_1:
  %tmp91 = load i32, ptr %tmp86
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_Abs_str, i32 0, i32 0), i32 %tmp91)
  ret i32 %tmp91
}

@.fn_Loopy_str = private constant [6 x i8] c"Loopy\00"
define i32 @Loopy(i32 %value) {
entry:
  %tmp92 = alloca i32
  store i32 %value, ptr %tmp92
  %i_Loopy = alloca i32
  store i32 0, ptr %i_Loopy
  br label %for_cond2
for_cond2:
  %tmp93 = load i32, ptr %i_Loopy
  %tmp94 = load i32, ptr %tmp92
  %tmp95 = icmp slt i32 %tmp93, %tmp94

  br i1 %tmp95, label %for_body2, label %for_end2
for_body2:
  %tmp96 = load i32, ptr %tmp92
  %tmp97 = load i32, ptr %i_Loopy
  %tmp98 = add i32 %tmp96, %tmp97

  store i32 %tmp98, ptr %tmp92
  br label %for_iter2
for_iter2:
  %tmp99 = load i32, ptr %i_Loopy
  %tmp100 = sub i32 %tmp99, 3

  store i32 %tmp100, ptr %i_Loopy
  br label %for_cond2
for_end2:
  %tmp101 = load i32, ptr %tmp92
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_Loopy_str, i32 0, i32 0), i32 %tmp101)
  ret i32 %tmp101
}

@.fn_n_str = private constant [2 x i8] c"n\00"
define i1 @n(i1 %b) {
entry:
  %tmp102 = alloca i1
  store i1 %b, ptr %tmp102
  %tmp103 = load i1, ptr %tmp102
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([3 x i8], ptr @.fn_n_str, i32 0, i32 0), i1 %tmp103)
  ret i1 %tmp103
}

@.fn_m_str = private constant [2 x i8] c"m\00"
define i32 @m(i32 %j) {
entry:
  %tmp104 = alloca i32
  store i32 %j, ptr %tmp104
  %i_m = alloca i32
  store i32 3, ptr %i_m
  %tmp105 = load i32, ptr %tmp104
  %tmp106 = add i32 %tmp105, 4

  store i32 %tmp106, ptr %tmp104
  %health_m = alloca i32
  store i32 7, ptr %health_m
  %tmp107 = load i32, ptr %tmp104
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_m_str, i32 0, i32 0), i32 %tmp107)
  ret i32 %tmp107
}

@.fn_IsAlive_str = private constant [8 x i8] c"IsAlive\00"
define i1 @Entity.IsAlive(ptr %this) {
entry:
  %tmp108 = getelementptr %Entity, ptr %this, i32 0, i32 1
  %tmp109 = load float, ptr %tmp108
  %tmp110 = fcmp ogt float %tmp109, 0.0

  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([3 x i8], ptr @.fn_IsAlive_str, i32 0, i32 0), i1 %tmp110)
  ret i1 %tmp110
}

@.fn_AllocB_str = private constant [7 x i8] c"AllocB\00"
define ptr @AllocB(i64 %size) {
entry:
  %tmp111 = alloca i64
  store i64 %size, ptr %tmp111
  %tmp112 = load i64, ptr %tmp111
  %tmp113 = call ptr @__th_allocB(i64 %tmp112)
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_AllocB_str, i32 0, i32 0), ptr %tmp113)
  ret ptr %tmp113
}

@.fn_ReallocB_str = private constant [9 x i8] c"ReallocB\00"
define ptr @ReallocB(ptr %ptr, i64 %newSize) {
entry:
  %tmp114 = alloca ptr
  store ptr %ptr, ptr %tmp114
  %tmp115 = alloca i64
  store i64 %newSize, ptr %tmp115
  %tmp116 = load ptr, ptr %tmp114
  %tmp117 = load i64, ptr %tmp115
  %tmp118 = call ptr @__th_reallocB(ptr %tmp116, i64 %tmp117)
  call i32 (ptr, ...) @printf(ptr getelementptr inbounds ([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), ptr getelementptr inbounds ([4 x i8], ptr @.fn_ReallocB_str, i32 0, i32 0), ptr %tmp118)
  ret ptr %tmp118
}

@.fn_Free_str = private constant [5 x i8] c"Free\00"
define void @Free(ptr %ptr) {
entry:
  %tmp119 = alloca ptr
  store ptr %ptr, ptr %tmp119
  %tmp120 = load ptr, ptr %tmp119
  call void @__th_free(ptr %tmp120)
  ret void 
}

