; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"

%Transform = type { %Vector3, %Vector3, %Vector3 }
%Vector3 = type { float, float, float }

define i32 @main() {
entry:
  %position_main = alloca %Vector3
  %tmp2 = getelementptr %Vector3, %Vector3* %position_main, i32 0, i32 0
  store float -1.0, float* %tmp2
  %tmp3 = getelementptr %Vector3, %Vector3* %position_main, i32 0, i32 1
  store float -1.0, float* %tmp3
  %tmp4 = getelementptr %Vector3, %Vector3* %position_main, i32 0, i32 2
  store float -1.0, float* %tmp4

  %z_main = alloca float
  %tmp5 = getelementptr inbounds %Vector3, %Vector3* %position_main, i32 0, i32 2
  %tmp6 = load float, float* %tmp5
  store float %tmp6, float* %z_main
  %arr_main = alloca [3 x float]
  %tmp7 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 1
  %tmp8 = getelementptr inbounds %Vector3, %Vector3* %position_main, i32 0, i32 0
  %tmp9 = load float, float* %tmp8
  store float %tmp9, float* %tmp7
  %tmp10 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 2
  %tmp11 = getelementptr inbounds %Vector3, %Vector3* %position_main, i32 0, i32 1
  %tmp12 = load float, float* %tmp11
  store float %tmp12, float* %tmp10
  %tmp13 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 3
  %tmp14 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 1
  %tmp15 = load float, float* %tmp14
  store float %tmp15, float* %tmp13
  %negY_main = alloca float
  %tmp16 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 2
  %tmp17 = load float, float* %tmp16
  %tmp18 = fsub float 0.0, %tmp17
  store float %tmp18, float* %negY_main
  %position2_main = alloca %Vector3
  %tmp19 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 1
  %tmp20 = load float, float* %tmp19
  %tmp21 = getelementptr %Vector3, %Vector3* %position2_main, i32 0, i32 0
  store float %tmp20, float* %tmp21
  %tmp22 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 2
  %tmp23 = load float, float* %tmp22
  %tmp24 = fsub float 0.0, %tmp23
  %tmp25 = getelementptr %Vector3, %Vector3* %position2_main, i32 0, i32 1
  store float %tmp24, float* %tmp25
  %tmp26 = getelementptr inbounds %Vector3, %Vector3* %position_main, i32 0, i32 2
  %tmp27 = load float, float* %tmp26
  %tmp28 = getelementptr %Vector3, %Vector3* %position2_main, i32 0, i32 2
  store float %tmp27, float* %tmp28

  ret i32 0
}

