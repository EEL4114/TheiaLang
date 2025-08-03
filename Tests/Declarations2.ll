; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"

%Vector3 = type { float, float, float }

define i32 @main() {
entry:
  %position_main = alloca %Vector3
  %tmp0 = getelementptr %Vector3, %Vector3* %position_main, i32 0, i32 0
  store float -1.0, float* %tmp0
  %tmp1 = getelementptr %Vector3, %Vector3* %position_main, i32 0, i32 1
  store float -1.0, float* %tmp1
  %tmp2 = getelementptr %Vector3, %Vector3* %position_main, i32 0, i32 2
  store float -1.0, float* %tmp2

  %z_main = alloca float
  %tmp3 = getelementptr inbounds %Vector3, %Vector3* %position_main, i32 0, i32 2
  %tmp4 = load float, float* %tmp3
  store float %tmp4, float* %z_main
  %arr_main = alloca [3 x float]
  %tmp5 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 1
  %tmp6 = getelementptr inbounds %Vector3, %Vector3* %position_main, i32 0, i32 0
  %tmp7 = load float, float* %tmp6
  store float %tmp7, float* %tmp5
  %tmp8 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 2
  %tmp9 = getelementptr inbounds %Vector3, %Vector3* %position_main, i32 0, i32 1
  %tmp10 = load float, float* %tmp9
  store float %tmp10, float* %tmp8
  %tmp11 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 3
  %tmp12 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 1
  %tmp13 = load float, float* %tmp12
  store float %tmp13, float* %tmp11
  %negY_main = alloca float
  %tmp14 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 2
  %tmp15 = load float, float* %tmp14
  %tmp16 = fsub float 0.0, %tmp15
  store float %tmp16, float* %negY_main
  %position2_main = alloca %Vector3
  %tmp17 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 1
  %tmp18 = load float, float* %tmp17
  %tmp19 = getelementptr %Vector3, %Vector3* %position2_main, i32 0, i32 0
  store float %tmp18, float* %tmp19
  %tmp20 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 2
  %tmp21 = load float, float* %tmp20
  %tmp22 = fsub float 0.0, %tmp21
  %tmp23 = getelementptr %Vector3, %Vector3* %position2_main, i32 0, i32 1
  store float %tmp22, float* %tmp23
  %tmp24 = getelementptr inbounds %Vector3, %Vector3* %position_main, i32 0, i32 2
  %tmp25 = load float, float* %tmp24
  %tmp26 = getelementptr %Vector3, %Vector3* %position2_main, i32 0, i32 2
  store float %tmp25, float* %tmp26

  ret i32 0
}

