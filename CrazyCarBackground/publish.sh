#!/bin/bash
npm run build:prod

rm -rf crazy_car_background.zip

zip crazy_car_background.zip crazy_car_background/* crazy_car_background/static/* crazy_car_background/static/css/* crazy_car_background/static/fonts/* crazy_car_background/static/img/* crazy_car_background/static/js/*

scp crazy_car_background.zip tast_server:/data/wwwroot/default/

rm -rf crazy_car_background.zip

ssh -t tast_server "rm -rf /data/wwwroot/default/crazy_car_background"
ssh -t tast_server "unzip /data/wwwroot/default/crazy_car_background.zip -d /data/wwwroot/default/"

echo "Finish!!!"

