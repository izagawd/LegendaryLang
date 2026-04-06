fn foo(kk: &i32) -> &i32 {
    kk
}
fn main() -> i32 {
    let a = 55;
    *foo(&a)
}
