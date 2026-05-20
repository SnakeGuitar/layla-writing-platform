package com.layla.android.data.api

import com.layla.android.BuildConfig
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.atomic.AtomicReference

object NetworkConfig {
    private const val SCHEME = BuildConfig.BACKEND_SCHEME
    private const val HOST   = BuildConfig.BACKEND_HOST

    const val BASE_URL_CORE = "$SCHEME://$HOST:${BuildConfig.PORT_CORE}/"
    const val BASE_URL_WB   = "$SCHEME://$HOST:${BuildConfig.PORT_WB}/"

    // Hub roots (no trailing slash — SignalR builds the full URL itself).
    const val VOICE_HUB_BASE_URL    = "$SCHEME://$HOST:${BuildConfig.PORT_CORE}"
    const val PRESENCE_HUB_BASE_URL = "$SCHEME://$HOST:${BuildConfig.PORT_CORE}"
}

object RetrofitClient {
    private val logging = HttpLoggingInterceptor().apply {
        // Keep network logs out of release builds — token-bearing URLs leak via logcat otherwise.
        level = if (BuildConfig.DEBUG) HttpLoggingInterceptor.Level.BASIC
                else HttpLoggingInterceptor.Level.NONE
    }

    // AtomicReference instead of a plain mutable field: the interceptor runs on
    // OkHttp dispatcher threads while setToken() is called from the UI thread.
    private val tokenRef = AtomicReference<String?>(null)

    fun setToken(token: String?) {
        tokenRef.set(token)
    }

    fun getToken(): String? = tokenRef.get()

    private val authInterceptor = okhttp3.Interceptor { chain ->
        val request = tokenRef.get()?.let {
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
