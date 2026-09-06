-- ====================================================================
-- SUPABASE POSTGRESQL DATABASE DDL MIGRATION SCRIPT FOR DEBUG SDK
-- Host: db.cxxugsvxwkmlvcegkhkg.supabase.co
-- Database: postgres
-- User: postgres
-- ====================================================================

-- 1. Create Raw Events Table (sdk_events)
CREATE TABLE IF NOT EXISTS public.sdk_events (
    id VARCHAR(128) PRIMARY KEY,
    project_id VARCHAR(128) NOT NULL,
    session_id VARCHAR(128) NOT NULL,
    type VARCHAR(64) NOT NULL, -- Log, Crash, Network, Breadcrumb, ANR, Performance, Device
    timestamp BIGINT NOT NULL,
    payload TEXT NOT NULL DEFAULT '{}',
    device_info TEXT,
    user_id VARCHAR(128),
    created_utc TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- 2. Create Active Sessions Table (sdk_sessions)
CREATE TABLE IF NOT EXISTS public.sdk_sessions (
    session_id VARCHAR(128) PRIMARY KEY,
    project_id VARCHAR(128) NOT NULL DEFAULT 'default_project',
    user_id VARCHAR(128),
    device_model VARCHAR(256),
    app_version VARCHAR(64),
    start_time_utc TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    last_activity_utc TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    event_count INT DEFAULT 0 NOT NULL,
    has_crash BOOLEAN DEFAULT FALSE NOT NULL
);

-- 3. Create Crashes Indexing Table (sdk_crashes)
CREATE TABLE IF NOT EXISTS public.sdk_crashes (
    id VARCHAR(128) PRIMARY KEY,
    project_id VARCHAR(128) NOT NULL,
    session_id VARCHAR(128) NOT NULL,
    exception_name VARCHAR(256) NOT NULL,
    exception_message TEXT NOT NULL,
    stack_trace TEXT NOT NULL,
    breadcrumbs_json TEXT DEFAULT '[]',
    device_info_json TEXT DEFAULT '{}',
    timestamp BIGINT NOT NULL,
    created_utc TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- 4. Create App Logs Indexing Table (sdk_logs)
CREATE TABLE IF NOT EXISTS public.sdk_logs (
    id VARCHAR(128) PRIMARY KEY,
    project_id VARCHAR(128) NOT NULL,
    session_id VARCHAR(128) NOT NULL,
    level VARCHAR(32) NOT NULL DEFAULT 'INFO', -- DEBUG, INFO, WARNING, ERROR
    message TEXT NOT NULL,
    tag VARCHAR(128),
    timestamp BIGINT NOT NULL,
    created_utc TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- 5. Create Network Interceptor Logs Table (sdk_networks)
CREATE TABLE IF NOT EXISTS public.sdk_networks (
    id VARCHAR(128) PRIMARY KEY,
    project_id VARCHAR(128) NOT NULL,
    session_id VARCHAR(128) NOT NULL,
    method VARCHAR(16) NOT NULL DEFAULT 'GET',
    url TEXT NOT NULL,
    status_code INT,
    duration_ms BIGINT NOT NULL DEFAULT 0,
    error_message TEXT,
    timestamp BIGINT NOT NULL,
    created_utc TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- ====================================================================
-- PERFORMANCE INDEXES
-- ====================================================================

CREATE INDEX IF NOT EXISTS idx_sdk_events_session ON public.sdk_events(session_id);
CREATE INDEX IF NOT EXISTS idx_sdk_events_project ON public.sdk_events(project_id);
CREATE INDEX IF NOT EXISTS idx_sdk_events_type ON public.sdk_events(type);
CREATE INDEX IF NOT EXISTS idx_sdk_events_created ON public.sdk_events(created_utc DESC);

CREATE INDEX IF NOT EXISTS idx_sdk_sessions_last_act ON public.sdk_sessions(last_activity_utc DESC);
CREATE INDEX IF NOT EXISTS idx_sdk_crashes_created ON public.sdk_crashes(created_utc DESC);
CREATE INDEX IF NOT EXISTS idx_sdk_logs_created ON public.sdk_logs(created_utc DESC);
CREATE INDEX IF NOT EXISTS idx_sdk_networks_created ON public.sdk_networks(created_utc DESC);
