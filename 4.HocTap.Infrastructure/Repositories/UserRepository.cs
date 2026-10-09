using HocTap.Domain.Aggregates.Auth;
using HocTap.Infrastructure.Entities;
using HocTap.Infrastructure.MappingRepos;
using HocTap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HocTap.Infrastructure.Repositories;

/// <summary>
/// 08/10/2026 - Repository tài khoản + refresh token.
/// Đọc: AsNoTracking -> map sang Model. Ghi: map Model -> entity rồi Add / Update vào DbContext (Service gọi unitOfWork.Save()).
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly HocTapDbContext dbContext;

    public UserRepository(HocTapDbContext i_context)
    {
        dbContext = i_context;
    }

    public AppUserModel? GetUserById(Guid i_id)
    {
        app_user? entity = dbContext.app_user.AsNoTracking().FirstOrDefault(x => x.id == i_id);
        return entity == null ? null : HocTapEntityMapper.ToModel(entity);
    }

    public AppUserModel? GetUserByEmail(string i_email)
    {
        app_user? entity = dbContext.app_user.AsNoTracking().FirstOrDefault(x => x.email == i_email);
        return entity == null ? null : HocTapEntityMapper.ToModel(entity);
    }

    public AppUserModel? GetUserByPhone(string i_phone)
    {
        app_user? entity = dbContext.app_user.AsNoTracking().FirstOrDefault(x => x.phone == i_phone);
        return entity == null ? null : HocTapEntityMapper.ToModel(entity);
    }

    public void InsertUser(AppUserModel i_user)
    {
        dbContext.app_user.Add(HocTapEntityMapper.ToEntity(i_user));
    }

    public void UpdateUser(AppUserModel i_user)
    {
        dbContext.app_user.Update(HocTapEntityMapper.ToEntity(i_user));
    }

    public RefreshTokenModel? GetRefreshTokenByHash(string i_tokenHash)
    {
        refresh_token? entity = dbContext.refresh_token.AsNoTracking().FirstOrDefault(x => x.tokenhash == i_tokenHash);
        return entity == null ? null : HocTapEntityMapper.ToModel(entity);
    }

    public void InsertRefreshToken(RefreshTokenModel i_token)
    {
        dbContext.refresh_token.Add(HocTapEntityMapper.ToEntity(i_token));
    }

    public void UpdateRefreshToken(RefreshTokenModel i_token)
    {
        dbContext.refresh_token.Update(HocTapEntityMapper.ToEntity(i_token));
    }

    /// <summary>Đánh dấu thu hồi mọi refresh token còn hiệu lực của tài khoản (lưu khi Service gọi Save)</summary>
    public void RevokeAllRefreshToken(Guid i_userId)
    {
        List<refresh_token> lstToken = dbContext.refresh_token.Where(x => x.userid == i_userId && !x.revoked).ToList();
        foreach (refresh_token token in lstToken)
        {
            token.revoked = true;
        }
    }
}
