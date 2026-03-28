fn bruh<'a>(dd: &'a i32, kk: &'a i32) -> &'a i32 {
    dd
}
fn main() -> i32 {
    let x = 10;
    let y = 20;
    let r = bruh(&x, &y);
    *r
}
