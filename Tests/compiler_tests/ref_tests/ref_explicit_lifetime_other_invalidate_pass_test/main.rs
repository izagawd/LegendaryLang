fn pick<'a>(dd: &'a i32, kk: &i32) -> &'a i32 {
    dd
}
fn main() -> i32 {
    let x = 10;
    let y = 20;
    let r = pick(&x, &y);
    &uniq y;
    *r
}
