chapter main
{
    while($SYSTEM_backlog_enable)
    {
        if(!EnableBacklog()||!$SYSTEM_backlog_enable)){
            break;
        }else if($SYSTEM_menu_close_enable){
            WaitKey();
        }
    }
}
