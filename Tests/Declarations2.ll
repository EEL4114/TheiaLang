; ModuleID = 'theia_module'
target triple = "x86_64-pc-windows-msvc19.44.35211"

%Transform = type { %Vector3, %Vector3, %Vector3 }
%Vector3 = type { float, float, float }

define i32 @main() {
entry:
  %position_main = alloca %Vector3
  %tmp15 = getelementptr %Vector3, %Vector3* %position_main, i32 0, i32 0
  store float -1.0, float* %tmp15
  %tmp16 = getelementptr %Vector3, %Vector3* %position_main, i32 0, i32 1
  store float -1.0, float* %tmp16
  %tmp17 = getelementptr %Vector3, %Vector3* %position_main, i32 0, i32 2
  store float -1.0, float* %tmp17

  %z_main = alloca float
  %tmp18 = getelementptr inbounds %Vector3, %Vector3* %position_main, i32 0, i32 2
  %tmp19 = load float, float* %tmp18
  store float %tmp19, float* %z_main
  %arr_main = alloca [3 x float]
  %tmp20 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 1
  %tmp21 = getelementptr inbounds %Vector3, %Vector3* %position_main, i32 0, i32 0
  %tmp22 = load float, float* %tmp21
  store float %tmp22, float* %tmp20
  %tmp23 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 2
  %tmp24 = getelementptr inbounds %Vector3, %Vector3* %position_main, i32 0, i32 1
  %tmp25 = load float, float* %tmp24
  store float %tmp25, float* %tmp23
  %tmp26 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 3
  %tmp27 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 1
  %tmp28 = load float, float* %tmp27
  store float %tmp28, float* %tmp26
  %negY_main = alloca float
  %tmp29 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 2
  %tmp30 = load float, float* %tmp29
  %tmp31 = fsub float 0.0, %tmp30
  store float %tmp31, float* %negY_main
  %position2_main = alloca %Vector3
  %tmp32 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 1
  %tmp33 = load float, float* %tmp32
  %tmp34 = getelementptr %Vector3, %Vector3* %position2_main, i32 0, i32 0
  store float %tmp33, float* %tmp34
  %tmp35 = getelementptr inbounds [3 x float], [3 x float]* %arr_main, i32 0, i32 2
  %tmp36 = load float, float* %tmp35
  %tmp37 = fsub float 0.0, %tmp36
  %tmp38 = getelementptr %Vector3, %Vector3* %position2_main, i32 0, i32 1
  store float %tmp37, float* %tmp38
  %tmp39 = getelementptr inbounds %Vector3, %Vector3* %position_main, i32 0, i32 2
  %tmp40 = load float, float* %tmp39
  %tmp41 = getelementptr %Vector3, %Vector3* %position2_main, i32 0, i32 2
  store float %tmp40, float* %tmp41

  ret i32 0
}

