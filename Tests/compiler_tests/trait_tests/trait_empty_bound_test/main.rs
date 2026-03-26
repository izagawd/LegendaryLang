fn foo<T:>() -> i32 {
    5
}

fn main() -> i32 {
    foo::<i32>()
}
