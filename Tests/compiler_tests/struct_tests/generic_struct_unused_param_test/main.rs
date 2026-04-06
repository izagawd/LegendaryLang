struct Foo(T:! type) {
    val: i32
}

fn main() -> i32 {
    let f = make Foo(i32) { val : 5 };
    f.val
}
