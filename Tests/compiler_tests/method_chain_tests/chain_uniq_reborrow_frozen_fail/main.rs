// let idk = unique — reborrow freezes unique. Using unique while idk is live = error.
fn main() -> i32 {
    let x = 42;
    let unique = &uniq x;
    let idk = unique;
    *unique + *idk
}
