package com.tastsong.crazycar.controller;

import cn.hutool.core.util.ObjUtil;
import cn.hutool.core.util.StrUtil;
import com.tastsong.crazycar.dto.req.ReqUpdatingInfo;
import com.tastsong.crazycar.dto.resp.RespUpdatingInfo;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Scope;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.tastsong.crazycar.common.Result;
import com.tastsong.crazycar.common.ResultCode;
import com.tastsong.crazycar.service.ForcedUpdatingService;

import cn.hutool.json.JSONObject;
import lombok.extern.slf4j.Slf4j;

@RestController
@Scope("prototype")
@RequestMapping(value = "/v1")
@Slf4j
public class ForcedUpdatingController {
    @Autowired
    private ForcedUpdatingService forcedUpdatingService;

    @PostMapping(value = "/ForcedUpdating")
    public Object forcedUpdating(@RequestBody ReqUpdatingInfo req) throws Exception {
		if (!ObjUtil.isEmpty(req) && !StrUtil.isEmpty(req.getPlatform()) && !StrUtil.isEmpty(req.getVersion())) {
			String version = req.getVersion();
			String platform = req.getPlatform();
			log.info("ForcedUpdating version = " + version + "; platform = " + platform);
			RespUpdatingInfo resp = new RespUpdatingInfo();
			resp.is_forced_updating = forcedUpdatingService.isForcedUpdating(version, platform);
			resp.setUrl(forcedUpdatingService.getURL(platform));
			return resp;
		} else {
			return Result.failure(ResultCode.RC404);
		}		
    }
}
