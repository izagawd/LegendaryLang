trait C {}
trait B : C {}
trait A : B {}
impl B for i32 {}
impl A for i32 {}

fn main() -> i32 {
    5
}
