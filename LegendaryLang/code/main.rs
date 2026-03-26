
fn main() -> i32 {
    struct Wrapper<T> {
        val: T
    }

    impl<T: Copy> Copy for Wrapper<T> {
    }

 
    let a = Wrapper::<Wrapper<i32>> {
        val = Wrapper::<i32>{
            val = 5
            }
        };
        
    let b = a;
    let c = a;
    3
}