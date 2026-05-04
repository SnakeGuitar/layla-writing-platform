package com.layla.android.data.api

import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

object NetworkConfig {
    const val BASE_URL_CORE = "http://localhost:5287/"
    const val BASE_URL_WB = "http://localhost:3000/"    // Worldbuilding service (Node.js)
}

object RetrofitClient {
    private val logging = HttpLoggingInterceptor().apply {
        level = HttpLoggingInterceptor.Level.BASIC
    }

    private var _token: String? = null

    fun setToken(token: String?) {
        _token = token
    }

    private val authInterceptor = okhttp3.Interceptor { chain ->
        val request = _token?.let {
            chain.request().newBuilder()
                .addHeader("Authorization", "Bearer $it")
                .build()
        } ?: chain.request()
        chain.proceed(request)
    }

    private val okHttpClient = OkHttpClient.Builder()
        .addInterceptor(logging)
        .addInterceptor(authInterceptor)
        .build()

    private val retrofit by lazy {
        Retrofit.Builder()
            .baseUrl(NetworkConfig.BASE_URL_CORE)
            .client(okHttpClient)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
    }

    val authApiService: AuthApiService by lazy {
        retrofit.create(AuthApiService::class.java)
    }

    val projectApiService: ProjectApiService by lazy {
        retrofit.create(ProjectApiService::class.java)
    }

    private val retrofitWb by lazy {
        Retrofit.Builder()
            .baseUrl(NetworkConfig.BASE_URL_WB)
            .client(okHttpClient)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
    }

    val manuscriptApiService: ManuscriptApiService by lazy {
        retrofitWb.create(ManuscriptApiService::class.java)
    }

    val wikiApiService: WikiApiService by lazy {
        retrofitWb.create(WikiApiService::class.java)
    }
}
