fn pass_through(r: &i32) -> &i32 {
    r
}
fn main() -> i32 {
    let x = 10;
    let r1 = &x;
    let r2 = pass_through(r1);
    &uniq x;
    *r2
}
