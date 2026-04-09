// let idk = unique — reborrow. After idk's last use, unique is unfrozen (NLL).
fn main() -> i32 {
    let x = 42;
    let unique = &uniq x;
    let idk = unique;
    let val = *idk;
    // idk's last use was above — reborrow expired (NLL)
    *unique + val - 42
}
