struct Foo {
    val: i32
}

fn main() -> i32 {
    let gotten ={
        let a = 5;
        &a
        };
    *gotten
}
