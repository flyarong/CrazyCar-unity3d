package com.CrazyCarServer;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.PrintWriter;

import javax.servlet.ServletException;
import javax.servlet.annotation.WebServlet;
import javax.servlet.http.HttpServlet;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;

import com.alibaba.fastjson.JSONObject;

/**
 * Servlet implementation class Register
 */
@WebServlet("/Register")
public class Register extends HttpServlet {
	private static final long serialVersionUID = 1L;
       
    /**
     * @see HttpServlet#HttpServlet()
     */
    public Register() {
        super();
        // TODO Auto-generated constructor stub
    }

	/**
	 * @see HttpServlet#doGet(HttpServletRequest request, HttpServletResponse response)
	 */
	protected void doGet(HttpServletRequest request, HttpServletResponse response) throws ServletException, IOException {
		// TODO Auto-generated method stub
		response.setContentType("text/html");
		BufferedReader br = new BufferedReader(new InputStreamReader(request.getInputStream(),"utf-8"));
		String line = null;
		StringBuilder sb = new StringBuilder();
		while ((line = br.readLine()) != null) {
			sb.append(line);
		}
		System.out.println(sb.toString());
		
		PrintWriter out = response.getWriter();		
		//JSONArray jsonArray = new JSONArray();
        JSONObject jsonObject = new JSONObject();
		JSONObject json = JSONObject.parseObject(sb.toString());
		if(!sb.toString().isEmpty() && json.containsKey("UserName") && json.containsKey("Password")){
			//System.out.println(json.getString("UserName"));
			if (IsExistUser(json.getString("UserName"))){				
		        jsonObject.put("code", 423);
			} else{
				RegisterUser(json.getString("UserName"), json.getString("Password"));
				if (IsExistUser(json.getString("UserName"))){				
			        jsonObject.put("code", 200);
				} else{
			        jsonObject.put("code", 425);
				}
			}
		} else{
			jsonObject.put("code", 425);
		}
		
        //jsonArray.add(jsonObject);
	    //String jsonOutput = jsonArray.toJSONString();
		out.println(jsonObject.toString());	
		out.flush();
		out.close();
	}

	/**
	 * @see HttpServlet#doPost(HttpServletRequest request, HttpServletResponse response)
	 */
	protected void doPost(HttpServletRequest request, HttpServletResponse response) throws ServletException, IOException {
		// TODO Auto-generated method stub
		doGet(request, response);
	}
	
	private void RegisterUser(String userName, String password){
		String sql = "insert into all_user ( user_name, user_password, login_time ) values" +
                       "(\"" + userName + "\"," + "\"" + password + "\"," + System.currentTimeMillis()/1000 + ");";
		System.out.println(sql);
		Util.JDBC.ExecuteInsert(sql);
		return;
	}

	private boolean IsExistUser(String userName){
		String sql = "select user_password from all_user where user_name = "
				+ "\"" + userName + "\";";
		System.out.println(sql);
		String rs = Util.JDBC.ExecuteSelect(sql);
        return rs != null;		
	}
}