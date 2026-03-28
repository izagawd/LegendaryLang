trait Copy {}
impl Copy for i32 {}
impl Copy for bool {}
impl<T> Copy for &T {}
impl<T> Copy for &const T {}
impl<T> Copy for &mut T {}
