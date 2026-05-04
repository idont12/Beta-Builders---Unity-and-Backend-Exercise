from django.urls import include, path
from rest_framework.routers import DefaultRouter

from .views import LeaderboardView, ProfileViewSet, RegisterView

router = DefaultRouter()
router.register('profiles', ProfileViewSet, basename='profile')

urlpatterns = [
    path('leaderboard/', LeaderboardView.as_view(), name='leaderboard'),
    path('register/', RegisterView.as_view(), name='register'),
    path('', include(router.urls)),
]
