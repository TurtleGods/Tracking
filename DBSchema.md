Help me generate a PostgreSQL database schema.

I need 3 tables:

1. analytics_dashboards
2. tracking_events
3. tracking_sessions 
4. dashboard_editors
5. dashboard_favorites

### analytics_dashboards
- id: serial primary key
- email: varchar(255), unique
- password_hash: varchar(255)
- created_at: timestamptz default now()

### tracking_events
- id: uuid PK
- event_type: 
- event_name: 
- page_name: 
- component_name: 
- timestamp: timestamp
- refer
- expose_time: int4
- user_id
- company_id
- session_id
- device_type
- os_version
- browser_version
- network_type
- network_effective_type
- page_url
- page_title
- viewport_height
- properties jsonb

### tracking_sessions
- id: uuid
- user_id
- company_id
- started_at
- last_activity_at
- ended_at
- total_events
- total_views
- total_clicks
- total_exposes
- total_disapears
- device_type
- device_model
- entry_page
- exit_page
- created_at


### dashboard_editors
- id
- dashboard_id
- user_email
- added_at
- added_by

### dashboard_favorites
- id
- dashboard_id
- user_id
- created_at
