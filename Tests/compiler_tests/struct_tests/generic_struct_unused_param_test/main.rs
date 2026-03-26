struct Foo<T> {
    val: i32
}

fn main() -> i32 {
    let f = Foo::<i32> { val = 5 };
    f.val
}
