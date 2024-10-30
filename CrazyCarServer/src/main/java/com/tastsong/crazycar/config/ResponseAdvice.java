package com.tastsong.crazycar.config;

import java.util.LinkedHashMap;

import org.springframework.core.MethodParameter;
import org.springframework.http.MediaType;
import org.springframework.http.converter.HttpMessageConverter;
import org.springframework.http.server.ServerHttpRequest;
import org.springframework.http.server.ServerHttpResponse;
import org.springframework.lang.Nullable;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.servlet.mvc.method.annotation.ResponseBodyAdvice;

import com.tastsong.crazycar.common.Result;
import com.tastsong.crazycar.common.ResultCode;

import lombok.extern.slf4j.Slf4j;

@Slf4j
@RestControllerAdvice
public class ResponseAdvice implements ResponseBodyAdvice<Object> {
    @Override
    public boolean supports(MethodParameter returnType, Class<? extends HttpMessageConverter<?>> converterType) {
        return true;
    }
    @Override
    public Object beforeBodyWrite(@Nullable Object body, MethodParameter returnType, MediaType selectedContentType, 
        Class<? extends HttpMessageConverter<?>> selectedConverterType, ServerHttpRequest request, ServerHttpResponse response)  {
        if(body instanceof Result){
            return body;
        } else if (body instanceof LinkedHashMap) {
            // Spring boot 捕获异常
            log.info("beforeBodyWrite ResponseAdvice Type : " + body.getClass() + "; data : " + body.toString());
            return Result.failure(ResultCode.RC500);
        } else {
            //log.info("ResponseAdvice Type : " + body.getClass() + "; data : " + JSONUtil.toJsonStr(body));
            return Result.success(ResultCode.RC200.getMessage(), body);
        }
    }
}