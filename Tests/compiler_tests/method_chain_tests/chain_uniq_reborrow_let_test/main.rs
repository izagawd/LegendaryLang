// let idk = unique — reborrow, not move. idk is usable.
fn main() -> i32 {
    let x = 42;
    let unique = &uniq x;
    let idk = unique;
    *idk
}
