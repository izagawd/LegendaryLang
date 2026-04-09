fn foo(kk: &i32) -> &i32 {
    let r = kk;
    r
}
fn main() -> i32 {
    let a = 77;
    *foo(&a)
}
