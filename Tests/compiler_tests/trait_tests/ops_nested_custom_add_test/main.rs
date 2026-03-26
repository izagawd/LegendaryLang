fn main() -> i32 {
    struct Foo {}
    impl Add<i32> for Foo {
        type Output = i32;
        fn add(dd: Foo, other: i32) -> i32 {
            other
        }
    }
    let a: i32 = Foo{} + 5;
    a
}
