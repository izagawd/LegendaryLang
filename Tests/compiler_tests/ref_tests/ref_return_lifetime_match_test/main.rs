fn bro['a, 'b](dd: &'a i32, kk: &'b i32) -> &'b i32 {
    kk
}
fn main() -> i32 {
    let x = 10;
    let y = 20;
    let r = bro(&x, &y);
    *r
}
