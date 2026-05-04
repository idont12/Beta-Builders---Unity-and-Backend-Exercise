from rest_framework import mixins, status, viewsets
from rest_framework.decorators import action
from rest_framework.permissions import AllowAny
from rest_framework.response import Response
from rest_framework.views import APIView

from .models import Profile
from .serializers import (
    LeaderboardEntrySerializer,
    ProfileSerializer,
    RegisterSerializer,
)


class ProfileViewSet(
    mixins.ListModelMixin,
    mixins.RetrieveModelMixin,
    viewsets.GenericViewSet,
):
    queryset = Profile.objects.select_related('user').all()
    serializer_class = ProfileSerializer

    @action(detail=False, methods=['get', 'patch'], url_path='me')
    def me(self, request):
        profile, _ = Profile.objects.select_related('user').get_or_create(
            user=request.user
        )

        if request.method == 'GET':
            return Response(self.get_serializer(profile).data)

        serializer = self.get_serializer(profile, data=request.data, partial=True)
        serializer.is_valid(raise_exception=True)
        serializer.save()
        return Response(serializer.data)


class LeaderboardView(APIView):
    permission_classes = [AllowAny]

    def get(self, request):
        qs = (
            Profile.objects.select_related('user')
            .order_by('-score', 'user__username')[:10]
        )
        data = LeaderboardEntrySerializer(qs, many=True).data
        return Response(data)


class RegisterView(APIView):
    permission_classes = [AllowAny]

    def post(self, request):
        serializer = RegisterSerializer(data=request.data)
        serializer.is_valid(raise_exception=True)
        user = serializer.save()
        profile = Profile.objects.select_related('user').get(user=user)
        return Response(
            ProfileSerializer(profile).data,
            status=status.HTTP_201_CREATED,
        )
