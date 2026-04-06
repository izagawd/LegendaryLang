trait B {}
trait C {}
trait A : B + C {}
impl B for i32 {}
impl A for i32 {}

fn main() -> i32 {
    5
}
